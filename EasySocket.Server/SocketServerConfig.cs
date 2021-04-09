namespace EasySocket.Server
{
    public class SocketServerConfig : ISocketServerConfig
    {
        public int maxConnection { get; set; } = 10000;

        public int recvBufferSize { get; set; } = 1024 * 4;

        public int sendBufferSize { get; set; } = 1024 * 4;

        public int recvTimeout { get; set; } = 0;

        public int sendTimeout { get; set; } = 0;

        public bool noDelay { get; set; } = true;

        public ISocketServerConfig DeepClone()
        {
            return new SocketServerConfig
            {
                maxConnection = maxConnection
        };
        }
    }
}