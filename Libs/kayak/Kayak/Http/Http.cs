using Enumerable = System.Linq.Enumerable;

namespace Kayak.Http
{
    public struct HttpRequestHead
    {
        public string Method;
        public string Uri;
        public string Path;
        public string QueryString;
        public string Fragment;
        public System.Version Version;
        public System.Collections.Generic.IDictionary<string, string> Headers;

        public override string ToString()
        {
            return $"{Method} {Uri}\r\n{(Headers != null ? Enumerable.Aggregate(Headers, "", (acc, kv) => acc + $"{kv.Key}: {kv.Value}\r\n") : "")}\r\n";
        }
    }

    public struct HttpResponseHead
    {
        public string Status;
        public System.Collections.Generic.IDictionary<string, string> Headers;

        public override string ToString()
        {
            return $"{Status}\r\n{(Headers != null ? Enumerable.Aggregate(Headers, "", (acc, kv) => acc + $"{kv.Key}: {kv.Value}\r\n") : "")}\r\n";
        }
    }

    public interface IHttpServerFactory
    {
        Net.IServer Create(IHttpRequestDelegate del, Net.IScheduler scheduler);
    }

    public interface IHttpRequestDelegate
    {
        void OnRequest(HttpRequestHead head, Net.IDataProducer body, IHttpResponseDelegate response);
    }

    public interface IHttpResponseDelegate
    {
        void OnResponse(HttpResponseHead head, Net.IDataProducer body);
    }

    internal interface IHttpServerTransaction : System.IDisposable
    {
        System.Net.IPEndPoint RemoteEndPoint { get; }
        void OnResponse(HttpResponseHead response);
        bool OnResponseData(System.ArraySegment<byte> data, System.Action continuation);
        void OnResponseEnd();
        void OnEnd();
    }

    internal interface IHttpServerTransactionDelegate
    {
        void OnRequest(IHttpServerTransaction transaction, HttpRequestHead request, bool shouldKeepAlive);

        bool OnRequestData(IHttpServerTransaction transaction, System.ArraySegment<byte> data,
            System.Action continuation);

        void OnRequestEnd(IHttpServerTransaction transaction);
        void OnError(IHttpServerTransaction transaction, System.Exception e);
        void OnEnd(IHttpServerTransaction transaction);
        void OnClose(IHttpServerTransaction transaction);
    }
}