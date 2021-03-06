using System;
using System.Buffers;
using System.Net.Sockets;
using EasySocket.Behaviors;
using EasySocket.SocketProxys;

namespace EasySocket.Workers
{
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
#region ISocketSessionWorker Field
        public ISocketServerWorker server { get; private set; } = null;
        public ISessionBehavior behavior { get; private set; } = null;
#endregion

        private ISocketProxy socketProxy = null;


#region ISocketSessionWorker Method
        public ISocketSessionWorker SetSessionBehavior(ISessionBehavior behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            this.behavior = behavior;

            return this;
        }
#endregion ISocketSessionWorker Method
    
        public void Initialize(ISocketServerWorker server, Socket socket)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            this.server = server;

            this.socketProxy = CreateSocketProxy();
            this.socketProxy.Initialize(socket);

            this.socketProxy.received = OnReceivedFromSocketProxy;
        }

        public void Start()
        {
            if (server == null)
            {
                throw new InvalidOperationException("Server not set : Please check if the session has been initialized.");
            }
            
            if (socketProxy == null)
            {
                throw new InvalidOperationException("SocketProxy not set : Please check if the session has been initialized.");
            }

            if (behavior == null)
            {
                throw new InvalidOperationException("SessionBehavior not set : Please call the \"SetSessionBehavior\" Method and set it up.");
            }

            socketProxy.Start();
        }

        private int OnReceivedFromSocketProxy(ref ReadOnlySequence<byte> sequence)
        {
            int readLength = 0;

            while (sequence.Length > readLength)
            {
                
            }

            return 0;
        }

        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 소켓 통신이 구현된 <see cref="ISocketProxy"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract ISocketProxy CreateSocketProxy();
    }
}