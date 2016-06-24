using Elpis.Kayak.Extensions;
using Elpis.Kayak.Http.Parsing;
using Elpis.Kayak.Net;

namespace Elpis.Kayak.Http
{
    // transforms socket events into http server transaction events.
    internal class HttpServerSocketDelegate : ISocketDelegate
    {
        public HttpServerSocketDelegate(IHttpServerTransaction transaction,
            IHttpServerTransactionDelegate transactionDelegate)
        {
            _transaction = transaction;
            _transactionDelegate = transactionDelegate;
            _transactionTransform = new ParserToTransactionTransform(transaction, transactionDelegate);
            _parser = new HttpMachine.HttpParser(new ParserDelegate(_transactionTransform));
        }

        private readonly HttpMachine.HttpParser _parser;
        private readonly IHttpServerTransaction _transaction;
        private readonly IHttpServerTransactionDelegate _transactionDelegate;
        private readonly ParserToTransactionTransform _transactionTransform;

        public bool OnData(ISocket socket, System.ArraySegment<byte> data, System.Action continuation)
        {
            try
            {
                int parsed = _parser.Execute(data);

                if (parsed == data.Count) return _transactionTransform.Commit(continuation);
                Trace.Write("Error while parsing request.");
                throw new System.Exception("Error while parsing request.");

                // raises request events on transaction delegate
            }
            catch (System.Exception e)
            {
                // XXX test this behavior
                OnError(socket, e);
                OnClose(socket);
                throw;
            }
        }

        public void OnEnd(ISocket socket)
        {
            System.Diagnostics.Debug.WriteLine("Socket OnEnd.");

            // parse EOF
            OnData(socket, default(System.ArraySegment<byte>), null);

            _transactionDelegate.OnEnd(_transaction);
        }

        public void OnError(ISocket socket, System.Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Socket OnError.");
            Elpis.Kayak.Extensions.Extensions.DebugStackTrace(e);
            _transactionDelegate.OnError(_transaction, e);
        }

        public void OnClose(ISocket socket)
        {
            _transactionDelegate.OnClose(_transaction);
        }

        public void OnConnected(ISocket socket)
        {
            throw new System.NotImplementedException();
        }
    }
}