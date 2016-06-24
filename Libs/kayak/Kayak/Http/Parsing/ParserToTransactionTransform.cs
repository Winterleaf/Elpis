namespace Kayak.Http.Parsing
{
    // adapts synchronous parser events to asynchronous "socket-like" events.
    // 
    // in so doing it introduces the backpressure mechanism to support the 
    // OnData event. this requires a "commit" phase after all the data 
    // currently in memory has been fed through the parser.
    // 
    // bundles data events such that if more events are queued, the next event 
    // cannot be deferred (i.e., ensures continuation is null). essentially, 
    // this makes sure that no backpressure is applied in the middle of a 
    // single read when events remain to be dealt with.
    // 
    // for example, if the server gets a packet with two requests in it, and 
    // the first request has an entity body, the user cannot expect to "delay" 
    // the processing of the next request by returning true from the OnData 
    // handler, since that request is already in memory.
    internal class ParserToTransactionTransform : Parsing.IHighLevelParserDelegate
    {
        public ParserToTransactionTransform(IHttpServerTransaction transaction,
            IHttpServerTransactionDelegate transactionDelegate)
        {
            _transaction = transaction;
            _transactionDelegate = transactionDelegate;
            _queue = new Parsing.ParserEventQueue();
        }

        private readonly Parsing.ParserEventQueue _queue;
        private readonly IHttpServerTransaction _transaction;
        private readonly IHttpServerTransactionDelegate _transactionDelegate;

        public void OnRequestBegan(Parsing.HttpRequestHeaders head, bool shouldKeepAlive)
        {
            _queue.OnRequestBegan(head, shouldKeepAlive);
        }

        public void OnRequestBody(System.ArraySegment<byte> data)
        {
            _queue.OnRequestBody(data);
        }

        public void OnRequestEnded()
        {
            _queue.OnRequestEnded();
        }

        public bool Commit(System.Action continuation)
        {
            while (_queue.HasEvents)
            {
                Parsing.ParserEvent e = _queue.Dequeue();

                switch (e.Type)
                {
                    case Parsing.ParserEventType.RequestHeaders:
                        _transactionDelegate.OnRequest(_transaction,
                            new HttpRequestHead
                            {
                                Method = e.Request.Method,
                                Uri = e.Request.Uri,
                                Path = e.Request.Path,
                                Fragment = e.Request.Fragment,
                                QueryString = e.Request.QueryString,
                                Version = e.Request.Version,
                                Headers = e.Request.Headers
                            }, e.KeepAlive);
                        break;
                    case Parsing.ParserEventType.RequestBody:
                        if (!_queue.HasEvents)
                            return _transactionDelegate.OnRequestData(_transaction, e.Data, continuation);

                        _transactionDelegate.OnRequestData(_transaction, e.Data, null);
                        break;
                    case Parsing.ParserEventType.RequestEnded:
                        _transactionDelegate.OnRequestEnd(_transaction);
                        break;
                }
            }
            return false;
        }
    }
}