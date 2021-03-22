namespace EasySocket.Client
{
    public class SocketClientConfig
    {
        public string ip { get; private set; } = string.Empty;
        public int port { get; private set; } = -1;

        public int sendBufferSize { get; private set; } = 1024 * 4;
        public int receiveBufferSize { get; private set; } = 1024 * 4;
    }
}