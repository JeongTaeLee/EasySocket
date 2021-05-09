namespace EasySocket.Client
{
    public class SocketClientConfig
    {
        public const int DefaultSendBufferSize = 1024 * 4;
        public const int DefaultReceiveBufferSize = 1204 * 4;

        public readonly int sendBufferSize;
        public readonly int receiveBufferSize;

        public SocketClientConfig(int sendBufferSize = DefaultSendBufferSize, int receiveBufferSize = DefaultReceiveBufferSize)
        {
            this.sendBufferSize = sendBufferSize;
            this.receiveBufferSize = receiveBufferSize;
        }
    }
}