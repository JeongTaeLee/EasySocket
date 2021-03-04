using System;
using EasySocket.Behaviors;
using EasySocket.SocketProxys;

namespace EasySocket.Workers
{
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
        public ISocketServerWorker server { get; private set; } = null;

        public ISocketProxy socketProxy { get; private set; } = null;

        public ISessionBehavior behavior { get; private set; } = null;

        public BaseSocketSessionWorker(ISocketServerWorker server, ISocketProxy socketProxy)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (socketProxy == null)
            {
                throw new ArgumentNullException(nameof(socketProxy));
            }

            this.server = server;
            this.socketProxy = socketProxy;
        }

        /// <summary>
        /// <see cref="ISocketSessionWorker"/> 를 시작합니다.
        /// </summary>
        public void Start()
        {
            if (behavior == null)
            {
                throw new InvalidOperationException("SessionBehavior not set : Please call the \"SetSessionBehavior\" Method and set it up.");
            }

            socketProxy.Start();
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