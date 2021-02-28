using System;
namespace EasySocket.Workers
{
    public class ServerWorkerConfig : IServerWorkerConfig
    {
        public int sendBufferSize { get; private set; }

        public int recvBufferSize { get; private set; }

        public bool noDelay { get; private set; }
    }
}
