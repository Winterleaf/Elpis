namespace Kayak.Net.Socket
{
    internal class KayakSocketState
    {
        public KayakSocketState(bool connected)
        {
            _state = connected ? State.NotConnected : State.Connected;
        }

        private State _state;

        public void SetConnecting()
        {
            if ((_state & State.Disposed) > 0)
                throw new System.ObjectDisposedException(typeof (KayakSocket).Name);

            if ((_state & State.Connected) > 0)
                throw new System.InvalidOperationException("The socket was connected.");

            if ((_state & State.Connecting) > 0)
                throw new System.InvalidOperationException("The socket was connecting.");

            _state |= State.Connecting;
        }

        public void SetConnected()
        {
            // these checks should never pass; they are here for safety.
            if ((_state & State.Disposed) > 0)
                throw new System.ObjectDisposedException(typeof (KayakSocket).Name);

            if ((_state & State.Connecting) == 0)
                throw new System.Exception("The socket was not connecting.");

            _state ^= State.Connecting;
            _state |= State.Connected;
        }

        public void BeginWrite(bool nonZeroData)
        {
            if ((_state & State.Disposed) > 0)
                throw new System.ObjectDisposedException("KayakSocket");

            if ((_state & State.Connected) == 0)
                throw new System.InvalidOperationException("The socket was not connected.");

            if ((_state & State.WriteEnded) > 0)
                throw new System.InvalidOperationException("The socket was previously ended.");

            if (nonZeroData)
                _state |= State.BufferIsNotEmpty;
        }

        private void CanShutdownAndClose(out bool shutdownSocket, out bool raiseClosed)
        {
            bool bufferIsEmpty = (_state & State.BufferIsNotEmpty) == 0;
            bool readEnded = (_state & State.ReadEnded) > 0;
            bool writeEnded = (_state & State.WriteEnded) > 0;

            System.Diagnostics.Debug.WriteLine("KayakSocketState: CanShutdownAndClose (readEnded = " + readEnded +
                                               ", writeEnded = " + writeEnded + ", bufferIsEmpty = " + bufferIsEmpty +
                                               ")");

            shutdownSocket = writeEnded && bufferIsEmpty;

            if (readEnded && shutdownSocket)
            {
                _state |= State.Closed;
                raiseClosed = true;
            }
            else
                raiseClosed = false;
        }

        public void EndWrite(bool bufferIsEmpty, out bool shutdownSocket, out bool raiseClosed)
        {
            if (bufferIsEmpty)
                _state ^= State.BufferIsNotEmpty;

            CanShutdownAndClose(out shutdownSocket, out raiseClosed);
        }

        // okay, so.
        //
        // need to check this every time we're about to do a read.
        // since we potentially do this in a loop, we return false
        // to indicate that the loop should break out. however, if the 
        // socket was never connected...well, that's an error, bro.
        public bool CanRead()
        {
            if ((_state & State.Connected) == 0)
                throw new System.InvalidOperationException("The socket was not connected.");

            return (_state & State.ReadEnded) <= 0;
        }

        public void SetReadEnded(out bool raiseClosed)
        {
            _state |= State.ReadEnded;

            if ((_state & State.WriteEnded) > 0 && (_state & State.BufferIsNotEmpty) == 0)
            {
                _state |= State.Closed;
                raiseClosed = true;
            }
            else
                raiseClosed = false;
        }

        public void SetEnded(out bool shutdownSocket, out bool raiseClosed)
        {
            if ((_state & State.Disposed) > 0)
                throw new System.ObjectDisposedException(typeof (KayakSocket).Name);

            if ((_state & State.Connected) == 0)
                throw new System.InvalidOperationException("The socket was not connected.");

            if ((_state & State.WriteEnded) > 0)
                throw new System.InvalidOperationException("The socket was previously ended.");

            _state |= State.WriteEnded;

            CanShutdownAndClose(out shutdownSocket, out raiseClosed);
        }

        public void SetError()
        {
            if ((_state & State.Disposed) > 0)
                throw new System.ObjectDisposedException(typeof (KayakSocket).Name);

            _state ^= State.Connecting | State.Connected;
            _state |= State.Closed;
        }

        public void SetDisposed()
        {
            //if ((state & State.Disposed) > 0)
            //    throw new ObjectDisposedException(typeof(KayakSocket).Name);

            _state |= State.Disposed;
        }

        [System.Flags]
        private enum State
        {
            NotConnected = 1,
            Connecting = 1 << 1,
            Connected = 1 << 2,
            WriteEnded = 1 << 3,
            ReadEnded = 1 << 4,
            Closed = 1 << 5,
            Disposed = 1 << 6,
            BufferIsNotEmpty = 1 << 7
        }
    }
}