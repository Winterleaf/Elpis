 // XXX all of this should live in HTTP Machine

namespace Elpis.Kayak.Http.Parsing
{
    internal struct HttpRequestHeaders
    {
        public string Method;
        public string Uri;
        public string Path;
        public string QueryString;
        public string Fragment;
        public System.Version Version;
        public System.Collections.Generic.IDictionary<string, string> Headers;
    }

    internal interface IHighLevelParserDelegate
    {
        void OnRequestBegan(HttpRequestHeaders request, bool shouldKeepAlive);
        void OnRequestBody(System.ArraySegment<byte> data);
        void OnRequestEnded();
    }

    internal class ParserDelegate : HttpMachine.IHttpParserDelegate
    {
        public ParserDelegate(IHighLevelParserDelegate del)
        {
            _del = del;
        }

        private readonly IHighLevelParserDelegate _del;
        private System.Collections.Generic.IDictionary<string, string> _headers;
        private string _method;
        private string _requestUri;
        private string _path;
        private string _fragment;
        private string _queryString;
        private string _headerName;
        private string _headerValue;

        public void OnMessageBegin(HttpMachine.HttpParser parser)
        {
            _method = _requestUri = _path = _fragment = _queryString = _headerName = _headerValue = null;
            _headers = null;
        }

        public void OnMethod(HttpMachine.HttpParser parser, string method)
        {
            _method = method;
        }

        public void OnRequestUri(HttpMachine.HttpParser parser, string requestUri)
        {
            _requestUri = requestUri;
        }

        public void OnPath(HttpMachine.HttpParser parser, string path)
        {
            _path = path;
        }

        public void OnFragment(HttpMachine.HttpParser parser, string fragment)
        {
            _fragment = fragment;
        }

        public void OnQueryString(HttpMachine.HttpParser parser, string queryString)
        {
            _queryString = queryString;
        }

        public void OnHeaderName(HttpMachine.HttpParser parser, string name)
        {
            if (_headers == null)
                _headers =
                    new System.Collections.Generic.Dictionary<string, string>(
                        System.StringComparer.InvariantCultureIgnoreCase);

            if (!string.IsNullOrEmpty(_headerValue))
                CommitHeader();

            _headerName = name;
        }

        public void OnHeaderValue(HttpMachine.HttpParser parser, string value)
        {
            if (string.IsNullOrEmpty(_headerName))
                throw new System.Exception("Got header value without name.");

            _headerValue = value;
        }

        public void OnHeadersEnd(HttpMachine.HttpParser parser)
        {
            System.Diagnostics.Debug.WriteLine("OnHeadersEnd");

            if (!string.IsNullOrEmpty(_headerValue))
                CommitHeader();

            HttpRequestHeaders request = new HttpRequestHeaders
            {
                Method = _method,
                Path = _path,
                Fragment = _fragment,
                QueryString = _queryString,
                Uri = _requestUri,
                Headers = _headers,
                Version = new System.Version(parser.MajorVersion, parser.MinorVersion)
            };

            _del.OnRequestBegan(request, parser.ShouldKeepAlive);
        }

        public void OnBody(HttpMachine.HttpParser parser, System.ArraySegment<byte> data)
        {
            System.Diagnostics.Debug.WriteLine("OnBody");
            // XXX can we defer this check to the parser?
            if (data.Count > 0)
            {
                _del.OnRequestBody(data);
            }
        }

        public void OnMessageEnd(HttpMachine.HttpParser parser)
        {
            System.Diagnostics.Debug.WriteLine("OnMessageEnd");
            _del.OnRequestEnded();
        }

        private void CommitHeader()
        {
            _headers[_headerName] = _headerValue;
            _headerName = _headerValue = null;
        }
    }
}