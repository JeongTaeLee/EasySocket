namespace EasySocket.Workers
{
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
        public ISocketServerWorker server { get; private set; } = null;

        public BaseSocketSessionWorker(ISocketServerWorker server)
        {
            this.server = server;
        }
    }
}