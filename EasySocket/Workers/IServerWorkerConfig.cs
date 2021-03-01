namespace EasySocket.Workers
{
    public interface ISocketServerWorkerConfig
    {
        public int recvBufferSize {get;}
        public int sendBufferSize {get;}
        public int recvTimeout {get;}
        public int sendTimeout {get;}
        public bool noDelay {get;}
    }
}