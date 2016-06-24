using Enumerable = System.Linq.Enumerable;

namespace Kayak.Http
{
    internal class DataSubject : Net.IDataProducer, Net.IDataConsumer
    {
        public DataSubject(System.Func<System.IDisposable> disposable)
        {
            _disposable = disposable;
        }

        private readonly System.Func<System.IDisposable> _disposable;
        private DataBuffer _buffer;
        private Net.IDataConsumer _channel;

        private System.Action _continuation;
        private System.Exception _error;

        private bool _gotEnd;

        public void OnError(System.Exception e)
        {
            if (_channel == null)
                _error = e;
            else
                _channel.OnError(e);
        }

        public bool OnData(System.ArraySegment<byte> data, System.Action ack)
        {
            if (_channel != null) return _channel.OnData(data, ack);
            if (_buffer == null)
                _buffer = new DataBuffer();

            _buffer.Add(data);

            if (ack == null) return false;
            _continuation = ack;
            return true;
        }

        public void OnEnd()
        {
            if (_channel == null)
                _gotEnd = true;
            else
                _channel.OnEnd();
        }

        public System.IDisposable Connect(Net.IDataConsumer channel)
        {
            _channel = channel;

            if (_buffer != null)
            {
                _buffer.Each(d => channel.OnData(new System.ArraySegment<byte>(d), null));

                // XXX this maybe is kinda wrong.
                _continuation?.Invoke();
            }

            if (_error != null)
                channel.OnError(_error);

            if (_gotEnd)
                channel.OnEnd();

            return _disposable();
        }
    }

    internal class DataBuffer
    {
        private readonly System.Collections.Generic.List<byte[]> _buffer = new System.Collections.Generic.List<byte[]>();

        public string GetString()
        {
            return Enumerable.Aggregate(_buffer, "", (acc, next) => acc + System.Text.Encoding.UTF8.GetString(next));
        }

        public int GetCount()
        {
            return Enumerable.Aggregate(_buffer, 0, (c, d) => c + d.Length);
        }

        public void Add(System.ArraySegment<byte> d)
        {
            // XXX maybe we should have our own allocator? i don't know. maybe the runtime is good
            // about this in awesome ways.
            byte[] b = new byte[d.Count];
            System.Buffer.BlockCopy(d.Array, d.Offset, b, 0, d.Count);
            _buffer.Add(b);
        }

        public void Each(System.Action<byte[]> each)
        {
            _buffer.ForEach(each);
        }
    }
}