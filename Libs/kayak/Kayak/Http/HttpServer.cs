namespace Kayak.Http
{
    public static class HttpServerExtensions
    {
        public static Net.IServer CreateHttp(this Net.IServerFactory factory, IHttpRequestDelegate channel, Net.IScheduler scheduler)
        {
            HttpServerFactory f = new HttpServerFactory(factory);
            return f.Create(channel, scheduler);
        }
    }

    internal class HttpServerFactory : IHttpServerFactory
    {
        public HttpServerFactory(Net.IServerFactory serverFactory)
        {
            _serverFactory = serverFactory;
        }

        private readonly Net.IServerFactory _serverFactory;

        public Net.IServer Create(IHttpRequestDelegate del, Net.IScheduler scheduler)
        {
            return _serverFactory.Create(new HttpServerDelegate(del), scheduler);
        }
    }

    internal class HttpServerDelegate : Net.IServerDelegate
    {
        public HttpServerDelegate(IHttpRequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        private readonly IHttpRequestDelegate _requestDelegate;

        public Net.ISocketDelegate OnConnection(Net.IServer server, Net.ISocket socket)
        {
            // XXX freelist
            HttpServerTransaction tx = new HttpServerTransaction(socket);
            HttpServerTransactionDelegate txDel = new HttpServerTransactionDelegate(_requestDelegate);
            HttpServerSocketDelegate socketDelegate = new HttpServerSocketDelegate(tx, txDel);
            return socketDelegate;
        }

        public void OnClose(Net.IServer server) {}
    }
}