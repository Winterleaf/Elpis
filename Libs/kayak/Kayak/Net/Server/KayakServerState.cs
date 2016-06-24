namespace Kayak.Net.Server
{
    internal class KayakServerState
    {
        public KayakServerState()
        {
            _state = State.None;
        }

        private int _connections;

        private State _state;

        public void SetListening()
        {
            lock (this)
            {
                if ((_state & State.Disposed) > 0)
                    throw new System.ObjectDisposedException(typeof (KayakServer).Name);

                if ((_state & State.Listening) > 0)
                    throw new System.InvalidOperationException("The server was already listening.");

                if ((_state & State.Closing) > 0)
                    throw new System.InvalidOperationException("The server was closing.");

                if ((_state & State.Closed) > 0)
                    throw new System.InvalidOperationException("The server was closed.");

                _state |= State.Listening;
            }
        }

        public void IncrementConnections()
        {
            lock (this)
            {
                _connections++;
            }
        }

        public bool DecrementConnections()
        {
            lock (this)
            {
                _connections--;

                if (_connections != 0 || (_state & State.Closing) <= 0) return false;
                _state ^= State.Closing;
                _state |= State.Closed;
                return true;
            }
        }

        public bool SetClosing()
        {
            lock (this)
            {
                if ((_state & State.Disposed) > 0)
                    throw new System.ObjectDisposedException(typeof (KayakServer).Name);

                if (_state == State.None)
                    throw new System.InvalidOperationException("The server was not listening.");

                if ((_state & State.Listening) == 0)
                    throw new System.InvalidOperationException("The server was not listening.");

                if ((_state & State.Closing) > 0)
                    throw new System.InvalidOperationException("The server was closing.");

                if ((_state & State.Closed) > 0)
                    throw new System.InvalidOperationException("The server was closed.");

                if (_connections == 0)
                {
                    _state |= State.Closed;
                    return true;
                }
                _state |= State.Closing;

                return true;
            }
        }

        public void SetError()
        {
            lock (this)
            {
                _state = State.Closed;
            }
        }

        public void SetDisposed()
        {
            lock (this)
            {
                if ((_state & State.Disposed) > 0)
                    throw new System.ObjectDisposedException(typeof (KayakServer).Name);

                _state |= State.Disposed;
            }
        }

        [System.Flags]
        private enum State
        {
            None = 0,
            Listening = 1,
            Closing = 1 << 1,
            Closed = 1 << 2,
            Disposed = 1 << 3
        }
    }
}