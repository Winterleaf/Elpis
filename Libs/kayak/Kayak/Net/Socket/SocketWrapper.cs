namespace Kayak
{
    // need to be able to get underneath the KayakSocket class for testing purposes.
    // kinda hanky, but should be able to swap it for a raw socket in the release build
    // using preprocessor macros...
    internal interface ISocketWrapper : System.IDisposable
    {
        System.Net.IPEndPoint RemoteEndPoint { get; }

        System.IAsyncResult BeginConnect(System.Net.IPEndPoint ep, System.AsyncCallback callback);
        void EndConnect(System.IAsyncResult iasr);

        System.IAsyncResult BeginReceive(byte[] buffer, int offset, int count, System.AsyncCallback callback);
        int EndReceive(System.IAsyncResult iasr);

        System.IAsyncResult BeginSend(System.Collections.Generic.List<System.ArraySegment<byte>> data,
            System.AsyncCallback callback);

        int EndSend(System.IAsyncResult iasr);

        void Shutdown();
    }

    internal class SocketWrapper : ISocketWrapper
    {
        public SocketWrapper(System.Net.Sockets.AddressFamily af)
            : this(
                new System.Net.Sockets.Socket(af, System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Tcp)) {}

        public SocketWrapper(System.Net.Sockets.Socket socket)
        {
            _socket = socket;
        }

        // perhaps a bit heavy-handed but no mono that can compile 4.0 .net works right anyway
        private static readonly bool SyncConnect = System.Environment.OSVersion.Platform == System.PlatformID.Unix;

        private System.Action<System.Net.IPEndPoint> _pendingConnect;
        private readonly System.Net.Sockets.Socket _socket;

        public System.Net.IPEndPoint RemoteEndPoint => (System.Net.IPEndPoint) _socket.RemoteEndPoint;

        public System.IAsyncResult BeginConnect(System.Net.IPEndPoint ep, System.AsyncCallback callback)
        {
            if (!SyncConnect) return _socket.BeginConnect(ep, callback, null);
            // voila, BeginConnect est borken avec mono 2.8-2.10. rad.
            // whatever it's probably implemented on a native threadpool anyway.
            _pendingConnect = _socket.Connect;
            return _pendingConnect.BeginInvoke(ep, callback, null);
        }

        public void EndConnect(System.IAsyncResult iasr)
        {
            if (SyncConnect)
            {
                _pendingConnect.EndInvoke(iasr);
            }
            else
                _socket.EndConnect(iasr);
        }

        public System.IAsyncResult BeginReceive(byte[] buffer, int offset, int count, System.AsyncCallback callback)
        {
            return _socket.BeginReceive(buffer, offset, count, System.Net.Sockets.SocketFlags.None, callback, null);
        }

        public int EndReceive(System.IAsyncResult iasr)
        {
            return _socket.EndReceive(iasr);
        }

        public System.IAsyncResult BeginSend(System.Collections.Generic.List<System.ArraySegment<byte>> data,
            System.AsyncCallback callback)
        {
            return _socket.BeginSend(data, System.Net.Sockets.SocketFlags.None, callback, null);
        }

        public int EndSend(System.IAsyncResult iasr)
        {
            return _socket.EndSend(iasr);
        }

        public void Shutdown()
        {
            _socket.Shutdown(System.Net.Sockets.SocketShutdown.Send);
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}