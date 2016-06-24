using Elpis.Kayak.Net;

namespace Elpis.Kayak.Http
{
    internal static class TransactionExtensions
    {
        public static void OnContinue(this IHttpServerTransaction transaction)
        {
            // write HTTP/1.1 100 Continue
            transaction.OnResponse(new HttpResponseHead {Status = "100 Continue"});
            transaction.OnResponseEnd();
        }
    }

    internal class HttpServerTransaction : IHttpServerTransaction
    {
        public HttpServerTransaction(ISocket socket)
        {
            _socket = socket;
        }

        private static readonly IHeaderRenderer DefaultRenderer = new HttpResponseHeaderRenderer();
        private readonly IHeaderRenderer _renderer = DefaultRenderer;
        private readonly ISocket _socket;

        public System.Net.IPEndPoint RemoteEndPoint => _socket.RemoteEndPoint;

        public void OnResponse(HttpResponseHead response)
        {
            _renderer.Render(_socket, response);
        }

        public bool OnResponseData(System.ArraySegment<byte> data, System.Action continuation)
        {
            return _socket.Write(data, continuation);
        }

        public void OnResponseEnd() {}

        public void OnEnd()
        {
            _socket.End();
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}