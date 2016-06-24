namespace Kayak.Net
{
    public interface ISchedulerFactory
    {
        IScheduler Create(ISchedulerDelegate del);
    }

    public interface IScheduler : System.IDisposable
    {
        void Post(System.Action action);
        void Start();
        void Stop();
    }

    public interface ISchedulerDelegate
    {
        void OnException(IScheduler scheduler, System.Exception e);
        void OnStop(IScheduler scheduler);
    }

    public static class KayakScheduler
    {
        public static ISchedulerFactory Factory { get; } = new DefaultKayakSchedulerFactory();
    }

    internal class DefaultKayakSchedulerFactory : ISchedulerFactory
    {
        public IScheduler Create(ISchedulerDelegate del)
        {
            return new Net.DefaultKayakScheduler(del);
        }
    }

    public interface IServerFactory
    {
        IServer Create(IServerDelegate del, IScheduler scheduler);
    }

    public interface IServer : System.IDisposable
    {
        System.IDisposable Listen(System.Net.IPEndPoint ep);
    }

    public interface IServerDelegate
    {
        ISocketDelegate OnConnection(IServer server, ISocket socket);
        void OnClose(IServer server);
    }

    public static class KayakServer
    {
        private static readonly DefaultKayakServerFactory factory = new DefaultKayakServerFactory();

        public static IServerFactory Factory => factory;
    }

    internal class DefaultKayakServerFactory : IServerFactory
    {
        public IServer Create(IServerDelegate del, IScheduler scheduler)
        {
            return new Net.Server.DefaultKayakServer(del, scheduler);
        }
    }

    public interface ISocketFactory
    {
        ISocket Create(ISocketDelegate del, IScheduler scheduler);
    }

    public interface ISocketDelegate
    {
        void OnConnected(ISocket socket);
        bool OnData(ISocket socket, System.ArraySegment<byte> data, System.Action continuation);
        void OnEnd(ISocket socket);
        void OnError(ISocket socket, System.Exception e);
        void OnClose(ISocket socket);
    }

    public interface ISocket : System.IDisposable
    {
        System.Net.IPEndPoint RemoteEndPoint { get; }
        void Connect(System.Net.IPEndPoint ep);
        bool Write(System.ArraySegment<byte> data, System.Action continuation);
        void End();
    }

    public static class KayakSocket
    {
        public static ISocketFactory Factory { get; } = new DefaultKayakSocketFactory();
    }

    internal class DefaultKayakSocketFactory : ISocketFactory
    {
        public ISocket Create(ISocketDelegate del, IScheduler scheduler)
        {
            return new Net.Socket.DefaultKayakSocket(del, scheduler);
        }
    }

    public interface IDataProducer
    {
        System.IDisposable Connect(IDataConsumer channel);
    }

    public interface IDataConsumer
    {
        void OnError(System.Exception e);
        bool OnData(System.ArraySegment<byte> data, System.Action continuation);
        void OnEnd();
    }
}