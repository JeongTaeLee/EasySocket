using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Common.Logging;

namespace EasySocket.SocketProxys
{
    public abstract class BaseSocketProxy : ISocketProxy
    {
        #region ISocketProxy Field
        public Socket socket { get; private set; }
        public SocketProxyReceiveHandler onReceived { get; set; }
        public SocketProxyErrorHandler onError { get; set; }
        public SocketProxyCloseHandler onClose { get; set; }
        #endregion

        protected ILogger logger { get; private set; } = null;

        #region ISocketProxy Method
        public virtual void Start(Socket sckt, ILogger lger)
        {
            socket = sckt ?? throw new ArgumentNullException(nameof(sckt));
            logger = lger ?? throw new ArgumentNullException(nameof(lger));
            
            if (onReceived == null)
            {
                logger.Warn("Received Handler is not set : Unable to receive events for socket receiving data.");
            }

            if (onError == null)
            {
                logger.Warn("Error Handler is not set : Unable to receive events for error.");
            }

            if (onClose == null)
            {
                logger.Warn("Close Handler is not set : Unable to receive events for close");
            }
        }
        
        public abstract void Stop();

        public abstract Task StopAsync();

        public abstract int Send(ReadOnlyMemory<byte> sendMmry);

        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMmry);
        #endregion
        
        /// <summary>
        /// 모든 종료 처리가 완료된 후 호출되야하는 함수입니다.
        /// </summary>
        protected virtual void OnStop()
        {
            socket?.Close();

            socket = null;
            logger = null;

            onReceived = null;
            onError = null;
            onClose = null;
        }
    }
}