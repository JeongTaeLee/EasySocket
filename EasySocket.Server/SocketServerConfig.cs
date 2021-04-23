using EasySocket.Common;

namespace EasySocket.Server
{
    public class SocketServerConfig : IDeepCloneableObject<SocketServerConfig>
    {
        public int maxConnection { get; set; } = 10000;

        public int recvBufferSize { get; set; } = 1024 * 4;

        public int sendBufferSize { get; set; } = 1024 * 4;

        public int recvTimeout { get; set; } = 0;

        public int sendTimeout { get; set; } = 0;

        public bool noDelay { get; set; } = true;

        public SocketServerConfig DeepClone()
        {
            return new SocketServerConfig
            {
                maxConnection = maxConnection,
                recvBufferSize = recvBufferSize,
                sendBufferSize = sendBufferSize,
                recvTimeout = recvTimeout,
                sendTimeout = sendTimeout,
                noDelay = noDelay
            };
        }
    }
}