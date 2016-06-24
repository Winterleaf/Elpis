namespace Kayak.Http.Parsing
{
    internal struct ParserEvent
    {
        public ParserEventType Type;
        public Parsing.HttpRequestHeaders Request;
        public bool KeepAlive;
        public System.ArraySegment<byte> Data;
    }

    internal enum ParserEventType
    {
        RequestHeaders,
        RequestBody,
        RequestEnded
    }

    internal class ParserEventQueue : Parsing.IHighLevelParserDelegate
    {
        public ParserEventQueue()
        {
            _events = new System.Collections.Generic.List<ParserEvent>();
        }

        public bool HasEvents => _events.Count > 0;

        private readonly System.Collections.Generic.List<ParserEvent> _events;

        public void OnRequestBegan(Parsing.HttpRequestHeaders request, bool shouldKeepAlive)
        {
            _events.Add(new ParserEvent
            {
                Type = ParserEventType.RequestHeaders,
                KeepAlive = shouldKeepAlive,
                Request = request
            });
        }

        public void OnRequestBody(System.ArraySegment<byte> data)
        {
            _events.Add(new ParserEvent {Type = ParserEventType.RequestBody, Data = data});
        }

        public void OnRequestEnded()
        {
            _events.Add(new ParserEvent {Type = ParserEventType.RequestEnded});
        }

        public ParserEvent Dequeue()
        {
            ParserEvent e = _events[0];
            _events.RemoveAt(0);
            return e;
        }
    }
}