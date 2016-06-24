namespace Kayak.Net.Server
{
    internal class DefaultKayakServer : IServer
    {
        internal DefaultKayakServer(IServerDelegate del, IScheduler scheduler)
        {
            if (del == null)
                throw new System.ArgumentNullException(nameof(del));

            if (scheduler == null)
                throw new System.ArgumentNullException(nameof(scheduler));

            _del = del;
            _scheduler = scheduler;
            _listener = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.IP);
            _state = new KayakServerState();
        }

        private readonly IServerDelegate _del;
        private readonly System.Net.Sockets.Socket _listener;

        private readonly IScheduler _scheduler;
        private readonly KayakServerState _state;

        public void Dispose()
        {
            _state.SetDisposed();

            _listener?.Dispose();
        }

        public System.IDisposable Listen(System.Net.IPEndPoint ep)
        {
            if (ep == null)
                throw new System.ArgumentNullException(nameof(ep));

            _state.SetListening();

            System.Diagnostics.Debug.WriteLine("KayakServer will bind to " + ep);

            _listener.Bind(ep);
            _listener.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                System.Net.Sockets.SocketOptionName.ReceiveTimeout, 10000);
            _listener.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                System.Net.Sockets.SocketOptionName.SendTimeout, 10000);
            _listener.Listen((int)System.Net.Sockets.SocketOptionName.MaxConnections);

            System.Diagnostics.Debug.WriteLine("KayakServer bound to " + ep);

            AcceptNext();
            return new Disposable(Close);
        }

        private void Close()
        {
            bool closed = _state.SetClosing();

            System.Diagnostics.Debug.WriteLine("Closing listening socket.");
            _listener.Close();

            if (closed)
                RaiseOnClose();
        }

        internal void SocketClosed(Socket.DefaultKayakSocket socket)
        {
            //Debug.WriteLine("Connection " + socket.id + ": closed (" + connections + " active connections)");
            if (_state.DecrementConnections())
                RaiseOnClose();
        }

        private void RaiseOnClose()
        {
            _del.OnClose(this);
        }

        private void AcceptNext()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("KayakServer: accepting connection");
                _listener.BeginAccept(iasr =>
                {
                    System.Diagnostics.Debug.WriteLine("KayakServer: accepted connection callback");
                    System.Net.Sockets.Socket socket = null;
                    System.Exception error = null;
                    try
                    {
                        if (_listener.Connected)
                            socket = _listener.EndAccept(iasr);
                        AcceptNext();
                    }
                    catch (System.Exception e)
                    {
                        error = e;
                    }

                    if (error is System.ObjectDisposedException)
                        return;

                    _scheduler.Post(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("KayakServer: accepted connection");
                        if (error != null)
                            HandleAcceptError(error);

                        Socket.DefaultKayakSocket s = new Socket.DefaultKayakSocket(new SocketWrapper(socket), _scheduler);
                        _state.IncrementConnections();

                        ISocketDelegate socketDelegate = _del.OnConnection(this, s);
                        s.Del = socketDelegate;
                        s.BeginRead();
                    });
                }, null);
            }
            catch (System.ObjectDisposedException) { }
            catch (System.Exception e)
            {
                HandleAcceptError(e);
            }
        }

        private void HandleAcceptError(System.Exception e)
        {
            _state.SetError();

            try
            {
                _listener.Close();
            }
            catch
            {
                //todo
            }

            System.Diagnostics.Debug.WriteLine("Error attempting to accept connection.");
            Extensions.Extensions.WriteStackTrace(System.Console.Error, e);

            RaiseOnClose();
        }
    }
}