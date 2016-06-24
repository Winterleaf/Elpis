using Elpis.Kayak.Net;

namespace Elpis.Kayak.Http
{
    public static class HttpServerExtensions
    {
        public static IServer CreateHttp(this IServerFactory factory, IHttpRequestDelegate channel, IScheduler scheduler)
        {
            HttpServerFactory f = new HttpServerFactory(factory);
            return f.Create(channel, scheduler);
        }
    }

    internal class HttpServerFactory : IHttpServerFactory
    {
        public HttpServerFactory(IServerFactory serverFactory)
        {
            _serverFactory = serverFactory;
        }

        private readonly IServerFactory _serverFactory;

        public IServer Create(IHttpRequestDelegate del, IScheduler scheduler)
        {
            return _serverFactory.Create(new HttpServerDelegate(del), scheduler);
        }
    }

    internal class HttpServerDelegate : IServerDelegate
    {
        public HttpServerDelegate(IHttpRequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        private readonly IHttpRequestDelegate _requestDelegate;

        public ISocketDelegate OnConnection(IServer server, ISocket socket)
        {
            // XXX freelist
            HttpServerTransaction tx = new HttpServerTransaction(socket);
            HttpServerTransactionDelegate txDel = new HttpServerTransactionDelegate(_requestDelegate);
            HttpServerSocketDelegate socketDelegate = new HttpServerSocketDelegate(tx, txDel);
            return socketDelegate;
        }

        public void OnClose(IServer server) {}
    }
}