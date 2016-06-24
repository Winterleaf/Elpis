namespace Kayak
{
    internal class Disposable : System.IDisposable
    {
        public Disposable(System.Action dispose)
        {
            _dispose = dispose;
        }

        private readonly System.Action _dispose;

        public void Dispose()
        {
            _dispose();
        }
    }
}