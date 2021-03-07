using System;
using System.Buffers;
using System.Net.Sockets;
using EasySocket.Behaviors;
using EasySocket.SocketProxys;
using EasySocket.Logging;

namespace EasySocket.Workers
{
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
#region ISocketSessionWorker Field
        public ISocketServerWorker server { get; private set; } = null;
        public ISessionBehavior behavior { get; private set; } = null;
#endregion

        private ISocketProxy socketProxy = null;
        protected ILogger logger { get; private set; } = null;

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
    
        public void Start(ISocketServerWorker server, Socket socket)
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
            logger = server.service.loggerFactroy.GetLogger(GetType());

            socketProxy = CreateSocketProxy();
            socketProxy.received = OnReceivedFromSocketProxy;
            socketProxy.error = OnErrorFromSocketProxy;

            socketProxy.Start(socket, server.service.loggerFactroy.GetLogger(socketProxy.GetType()));
            
            if (behavior != null)
            {
                behavior.OnStarted();
            }
        }

        private int OnReceivedFromSocketProxy(ref ReadOnlySequence<byte> sequence)
        {
            int readLength = 0;

            while (sequence.Length > readLength)
            {
                
            }

            return 0;
        }

        private void OnErrorFromSocketProxy(Exception ex)
        {

        }

        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 소켓 통신이 구현된 <see cref="ISocketProxy"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract ISocketProxy CreateSocketProxy();
    }
}