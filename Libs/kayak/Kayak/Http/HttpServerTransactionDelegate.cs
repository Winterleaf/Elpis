using Elpis.Kayak.Http.Extensions;
using Elpis.Kayak.Net;

namespace Elpis.Kayak.Http
{
    internal class HttpServerTransactionDelegate : IHttpServerTransactionDelegate
    {
        public HttpServerTransactionDelegate(IHttpRequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        private TransactionContext _currentContext;
        private ITransactionSegment _lastSegment;
        private readonly IHttpRequestDelegate _requestDelegate;

        public void OnRequest(IHttpServerTransaction transaction, HttpRequestHead request, bool shouldKeepAlive)
        {
            AddXFF(ref request, transaction.RemoteEndPoint);

            bool expectContinue = request.IsContinueExpected();
            bool ignoreResponseBody = request.Method != null && request.Method.ToUpperInvariant() == "HEAD";

            _currentContext = new TransactionContext(expectContinue, ignoreResponseBody, shouldKeepAlive);

            if (_lastSegment == null)
                _currentContext.Segment.AttachTransaction(transaction);

            QueueSegment(_currentContext.Segment);
            _requestDelegate.OnRequest(request, _currentContext.RequestBody, _currentContext);
        }

        public bool OnRequestData(IHttpServerTransaction transaction, System.ArraySegment<byte> data,
            System.Action continuation)
        {
            return _currentContext.RequestBody.OnData(data, continuation);
        }

        public void OnRequestEnd(IHttpServerTransaction transaction)
        {
            _currentContext.RequestBody.OnEnd();
        }

        public void OnError(IHttpServerTransaction transaction, System.Exception e)
        {
            _currentContext.RequestBody.OnError(e);
        }

        public void OnEnd(IHttpServerTransaction transaction)
        {
            QueueSegment(new EndSegment());
        }

        public void OnClose(IHttpServerTransaction transaction)
        {
            transaction.Dispose();
            // XXX return self to freelist
        }

        private void QueueSegment(ITransactionSegment segment)
        {
            _lastSegment?.AttachNext(segment);

            _lastSegment = segment;
        }

        private void AddXFF(ref HttpRequestHead request, System.Net.IPEndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null) return;
            if (request.Headers == null)
                request.Headers =
                    new System.Collections.Generic.Dictionary<string, string>(
                        System.StringComparer.InvariantCultureIgnoreCase);

            if (request.Headers.ContainsKey("X-Forwarded-For"))
            {
                request.Headers["X-Forwarded-For"] += "," + remoteEndPoint.Address;
            }
            else
            {
                request.Headers["X-Forwarded-For"] = remoteEndPoint.Address.ToString();
            }
        }

        private class TransactionContext : IHttpResponseDelegate
        {
            public TransactionContext(bool expectContinue, bool ignoreResponseBody, bool shouldKeepAlive)
            {
                RequestBody = new DataSubject(ConnectRequestBody);
                Segment = new ResponseSegment();

                _expectContinue = expectContinue;
                _ignoreResponseBody = ignoreResponseBody;
                _shouldKeepAlive = shouldKeepAlive;
            }

            private readonly bool _expectContinue;
            private readonly bool _ignoreResponseBody;
            private readonly bool _shouldKeepAlive;
            private bool _gotConnectRequestBody;
            public readonly DataSubject RequestBody;
            public readonly ResponseSegment Segment;

            public void OnResponse(HttpResponseHead head, IDataProducer body)
            {
                // XXX still need to better account for Connection: close.
                // this should cause the queue to drop pending responses
                // perhaps segment.Abort which disposes transation

                if (!_shouldKeepAlive)
                {
                    if (head.Headers == null)
                        head.Headers =
                            new System.Collections.Generic.Dictionary<string, string>(
                                System.StringComparer.InvariantCultureIgnoreCase);

                    head.Headers["Connection"] = "close";
                }

                Segment.WriteResponse(head, _ignoreResponseBody ? null : body);
            }

            private System.IDisposable ConnectRequestBody()
            {
                if (_gotConnectRequestBody)
                    throw new System.InvalidOperationException("Request body was already connected.");

                _gotConnectRequestBody = true;

                if (_expectContinue)
                    Segment.WriteContinue();

                return new Disposable(() =>
                {
                    // XXX what to do?
                    // ideally we stop reading from the socket. 
                    // equivalent to a parse error
                    // dispose transaction
                });
            }
        }
    }
}