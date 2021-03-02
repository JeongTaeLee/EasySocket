using System;
using System.Threading.Tasks;

namespace EasySocket.Workers
{
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
        public ISocketServerWorker server { get; private set; } = null;

        public virtual void Start(ISocketServerWorker server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            this.server = server;
        }

        public abstract void Send(ReadOnlyMemory<byte> sendMemory);
        public abstract ValueTask SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}