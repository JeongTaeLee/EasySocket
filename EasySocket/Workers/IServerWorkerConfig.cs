using System;

namespace EasySocket.Workers
{
    public interface IServerWorkerConfig
    {
        public int sendBufferSize { get; }

        public int recvBufferSize { get; }

        public bool noDelay { get; }
    }
}
