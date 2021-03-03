using System;
using System.Threading.Tasks;
using EasySocket.Behaviors;

namespace EasySocket.Workers
{
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
        public ISocketServerWorker server { get; private set; } = null;

        public ISessionBehavior behavior { get; private set; } = null;

        public BaseSocketSessionWorker(ISocketServerWorker server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            this.server = server;
        }

        public ISocketSessionWorker SetSessionBehavior(ISessionBehavior behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            this.behavior = behavior;

            return this;
        }
    }
}