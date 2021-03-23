namespace EasySocket.Client
{
    public class SocketClientConfig
    {
        public const int DefaultSendBufferSize = 1024 * 4;
        public const int DefaultReceiveBufferSize = 1204 * 4;

        public readonly string ip;
        public readonly int port;

        public readonly int sendBufferSize;
        public readonly int receiveBufferSize;

        public SocketClientConfig(string ip, int port, int sendBufferSize = DefaultSendBufferSize, int receiveBufferSize = DefaultReceiveBufferSize)
        {
            this.ip = ip;
            this.port = port;
            this.sendBufferSize = sendBufferSize;
            this.receiveBufferSize = receiveBufferSize;
        }
    }
}