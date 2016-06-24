namespace Kayak.Net.Socket
{
    internal class DefaultKayakSocket : ISocket
    {
        internal DefaultKayakSocket(ISocketDelegate del, IScheduler scheduler)
        {
            _scheduler = scheduler;
            Del = del;
            _state = new KayakSocketState(true);
        }

        internal DefaultKayakSocket(ISocketWrapper socket, IScheduler scheduler)
        {
            Id = _nextId++;
            _socket = socket;
            _scheduler = scheduler;
            _state = new KayakSocketState(false);
        }

        private static int _nextId;

        private OutputBuffer _buffer;
        private System.Action _continuation;
        internal ISocketDelegate Del;

        public int Id;

        private byte[] _inputBuffer;
        private readonly IScheduler _scheduler;

        private ISocketWrapper _socket;

        private readonly KayakSocketState _state;

        public System.Net.IPEndPoint RemoteEndPoint => _socket.RemoteEndPoint;

        public void Connect(System.Net.IPEndPoint ep)
        {
            _state.SetConnecting();

            System.Diagnostics.Debug.WriteLine("KayakSocket: connecting to " + ep);
            _socket = new SocketWrapper(ep.Address.AddressFamily);

            _socket.BeginConnect(ep, iasr =>
            {
                System.Diagnostics.Debug.WriteLine("KayakSocket: connected to " + ep);
                System.Exception error = null;

                try
                {
                    _socket.EndConnect(iasr);
                }
                catch (System.Exception e)
                {
                    error = e;
                }

                _scheduler.Post(() =>
                {
                    if (error is System.ObjectDisposedException)
                        return;

                    if (error != null)
                    {
                        _state.SetError();

                        System.Diagnostics.Debug.WriteLine("KayakSocket: error while connecting to " + ep);
                        RaiseError(error);
                    }
                    else
                    {
                        _state.SetConnected();

                        System.Diagnostics.Debug.WriteLine("KayakSocket: connected to " + ep);

                        Del.OnConnected(this);

                        BeginRead();
                    }
                });
            });
        }

        public bool Write(System.ArraySegment<byte> data, System.Action continuation)
        {
            _state.BeginWrite(data.Count > 0);

            if (data.Count == 0) return false;

            if (_continuation != null)
                throw new System.InvalidOperationException("Write was pending.");

            if (_buffer == null)
                _buffer = new OutputBuffer();

            int bufferSize = _buffer.Size;

            // XXX copy! could optimize here?
            _buffer.Add(data);
            System.Diagnostics.Debug.WriteLine("KayakSocket: added " + data.Count + " bytes to buffer, buffer size was " +
                                               bufferSize + ", buffer size is " + _buffer.Size);

            if (bufferSize > 0)
            {
                // we're between an async beginsend and endsend,
                // and user did not provide continuation

                if (continuation == null) return false;
                _continuation = continuation;
                return true;
            }
            bool result = BeginSend();

            // tricky: potentially throwing away fact that send will complete async
            if (continuation == null)
                result = false;

            if (result)
                _continuation = continuation;

            return result;
        }

        public void End()
        {
            System.Diagnostics.Debug.WriteLine("KayakSocket: end");

            bool shutdownSocket;
            bool raiseClosed;

            _state.SetEnded(out shutdownSocket, out raiseClosed);

            if (shutdownSocket)
            {
                System.Diagnostics.Debug.WriteLine("KayakSocket: shutting down socket on End.");
                _socket.Shutdown();
            }

            if (raiseClosed)
            {
                RaiseClosed();
            }
        }

        public void Dispose()
        {
            _state.SetDisposed();

            _socket?.Dispose();
        }

        internal void BeginRead()
        {
            if (_inputBuffer == null)
                _inputBuffer = new byte[1024*4];

            while (true)
            {
                if (!_state.CanRead()) return;

                int read;
                System.Exception error;
                System.IAsyncResult ar0;

                System.Diagnostics.Debug.WriteLine("KayakSocket: reading.");

                try
                {
                    ar0 = _socket.BeginReceive(_inputBuffer, 0, _inputBuffer.Length, ar =>
                    {
                        if (ar.CompletedSynchronously) return;

                        read = EndRead(ar, out error);

                        // small optimization
                        if (error is System.ObjectDisposedException)
                            return;

                        _scheduler.Post(() =>
                        {
                            System.Diagnostics.Debug.WriteLine("KayakSocket: receive completed async");

                            if (error != null)
                            {
                                HandleReadError(error);
                            }
                            else
                            {
                                if (!HandleReadResult(read, false))
                                    BeginRead();
                            }
                        });
                    });
                }
                catch (System.Exception e)
                {
                    HandleReadError(e);
                    break;
                }

                if (!ar0.CompletedSynchronously)
                    break;

                System.Diagnostics.Debug.WriteLine("KayakSocket: receive completed sync");
                read = EndRead(ar0, out error);

                if (error != null)
                {
                    HandleReadError(error);
                    break;
                }
                if (HandleReadResult(read, true))
                    break;
            }
        }

        private int EndRead(System.IAsyncResult ar, out System.Exception error)
        {
            error = null;
            try
            {
                return _socket.EndReceive(ar);
            }
            catch (System.Exception e)
            {
                error = e;
                return -1;
            }
        }

        private bool HandleReadResult(int read, bool sync)
        {
            System.Diagnostics.Debug.WriteLine("KayakSocket: " + (sync ? "" : "a") + "sync read " + read);

            if (read != 0) return Del.OnData(this, new System.ArraySegment<byte>(_inputBuffer, 0, read), BeginRead);
            PeerHungUp();
            return false;
        }

        private void HandleReadError(System.Exception e)
        {
            if (e is System.ObjectDisposedException)
                return;

            System.Diagnostics.Debug.WriteLine("KayakSocket: read error");

            if (e is System.Net.Sockets.SocketException)
            {
                System.Net.Sockets.SocketException socketException = e as System.Net.Sockets.SocketException;

                if (socketException.ErrorCode == 10053 || socketException.ErrorCode == 10054)
                {
                    System.Diagnostics.Debug.WriteLine("KayakSocket: peer reset (" + socketException.ErrorCode + ")");
                    PeerHungUp();
                    return;
                }
            }

            _state.SetError();

            RaiseError(new System.Exception("Error while reading.", e));
        }

        private void PeerHungUp()
        {
            System.Diagnostics.Debug.WriteLine("KayakSocket: peer hung up.");
            bool raiseClosed;
            _state.SetReadEnded(out raiseClosed);

            Del.OnEnd(this);

            if (raiseClosed)
                RaiseClosed();
        }

        private bool BeginSend()
        {
            while (true)
            {
                if (BufferIsEmpty())
                    break;

                int written = 0;
                System.Exception error;
                System.IAsyncResult ar0;

                try
                {
                    ar0 = _socket.BeginSend(_buffer.Data, ar =>
                    {
                        if (ar.CompletedSynchronously)
                            return;

                        written = EndSend(ar, out error);

                        // small optimization
                        if (error is System.ObjectDisposedException)
                            return;

                        _scheduler.Post(() =>
                        {
                            if (error != null)
                                HandleSendError(error);
                            else
                                HandleSendResult(written, false);

                            if (BeginSend() || _continuation == null) return;
                            System.Action c = _continuation;
                            _continuation = null;
                            c();
                        });
                    });
                }
                catch (System.Exception e)
                {
                    HandleSendError(e);
                    break;
                }

                if (!ar0.CompletedSynchronously)
                    return true;

                written = EndSend(ar0, out error);

                if (error != null)
                {
                    HandleSendError(error);
                    break;
                }
                HandleSendResult(written, true);
            }

            return false;
        }

        private int EndSend(System.IAsyncResult ar, out System.Exception error)
        {
            error = null;
            try
            {
                return _socket.EndSend(ar);
            }
            catch (System.Exception e)
            {
                error = e;
                return -1;
            }
        }

        private void HandleSendResult(int written, bool sync)
        {
            _buffer.Remove(written);

            System.Diagnostics.Debug.WriteLine("KayakSocket: Wrote " + written + " " + (sync ? "" : "a") +
                                               "sync, buffer size is " + _buffer.Size);

            bool shutdownSocket;
            bool raiseClosed;

            _state.EndWrite(BufferIsEmpty(), out shutdownSocket, out raiseClosed);

            if (shutdownSocket)
            {
                System.Diagnostics.Debug.WriteLine("KayakSocket: shutting down socket after send.");
                _socket.Shutdown();
            }

            if (raiseClosed)
                RaiseClosed();
        }

        private void HandleSendError(System.Exception error)
        {
            if (error is System.ObjectDisposedException) return;

            _state.SetError();
            RaiseError(new System.Exception("Exception on write.", error));
        }

        private bool BufferIsEmpty()
        {
            return _socket != null && (_buffer == null || _buffer.Size == 0);
        }

        private void RaiseError(System.Exception e)
        {
            System.Diagnostics.Debug.WriteLine("KayakSocket: raising OnError");
            Del.OnError(this, e);

            RaiseClosed();
        }

        private void RaiseClosed()
        {
            System.Diagnostics.Debug.WriteLine("KayakSocket: raising OnClose");
            Del.OnClose(this);
        }
    }
}