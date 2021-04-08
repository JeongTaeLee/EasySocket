namespace EasySocket.Server
{
    public class SocketConfig
    {
        public int recvBufferSize { get; private set; } = 1024 * 4;
        public int sendBufferSize { get; private set; } = 1024 * 4;

        public int recvTimeout { get; private set; } = 0;
        public int sendTimeout { get; private set; } = 0;

        public bool noDelay { get; private set; } = true;
    }
}