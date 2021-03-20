namespace EasySocket.Servers
{
    public interface ISocketServerConfig
    {
        public int recvBufferSize {get;}
        public int sendBufferSize {get;}
        public int recvTimeout {get;}
        public int sendTimeout {get;}
        public bool noDelay {get;}
    }
}