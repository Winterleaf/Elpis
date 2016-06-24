using Elpis.Kayak.Net;

namespace Elpis.Kayak.Http
{
    internal interface ITransactionSegment
    {
        void AttachNext(ITransactionSegment next);
        void AttachTransaction(IHttpServerTransaction transaction);
    }

    internal class ResponseSegment : ITransactionSegment, /*private*/ IDataConsumer
    {
        private IDataProducer _body;
        private bool _gotContinue;
        private bool _gotResponse;
        private bool _bodyFinished;
        private HttpResponseHead _head;

        private ITransactionSegment _next;

        private IHttpServerTransaction _transaction;

        public void OnError(System.Exception e)
        {
            _transaction.Dispose();
            _transaction = null;
            _next = null;
        }

        public bool OnData(System.ArraySegment<byte> data, System.Action continuation)
        {
            return _transaction.OnResponseData(data, continuation);
        }

        public void OnEnd()
        {
            _transaction.OnResponseEnd();

            _bodyFinished = true;

            HandOffTransactionIfPossible();
        }

        public void AttachNext(ITransactionSegment next)
        {
            _next = next;

            HandOffTransactionIfPossible();
        }

        public void AttachTransaction(IHttpServerTransaction transaction)
        {
            _transaction = transaction;

            if (_gotContinue)
                transaction.OnContinue();

            if (_gotResponse)
                DoWriteResponse();
        }

        public void WriteContinue()
        {
            if (_gotResponse) return;

            if (_gotContinue) throw new System.InvalidOperationException("WriteContinue was previously called.");
            _gotContinue = true;

            _transaction?.OnContinue();
        }

        public void WriteResponse(HttpResponseHead head, IDataProducer body)
        {
            if (_gotResponse) throw new System.InvalidOperationException("WriteResponse was previously called.");
            _gotResponse = true;

            _head = head;
            _body = body;

            if (_transaction != null)
                DoWriteResponse();
        }

        private void DoWriteResponse()
        {
            _transaction.OnResponse(_head);

            if (_body != null)
            {
                // XXX there is no cancel.
                _body.Connect(this);
            }
            else
            {
                _transaction.OnResponseEnd();
                HandOffTransactionIfPossible();
            }
        }

        private void HandOffTransactionIfPossible()
        {
            if (!_gotResponse || (_body != null && (_body == null || !_bodyFinished)) || _transaction == null || _next == null) return;
            _next.AttachTransaction(_transaction);
            _transaction = null;
            _next = null;
            _body = null;
        }
    }

    internal class EndSegment : ITransactionSegment
    {
        public void AttachNext(ITransactionSegment next) {}

        public void AttachTransaction(IHttpServerTransaction transaction)
        {
            transaction.OnEnd();
        }
    }
}