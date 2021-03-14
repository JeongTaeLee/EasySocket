using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Logging;

namespace EasySocket.SocketProxys
{
    public abstract class BaseSocketProxy : ISocketProxy
    {
#region ISocketProxy Field
        public Socket socket { get; private set; } = null;
        public SessionSocketProxyReceiveHandler onReceived { get; set; }
        public SessionSocketProxyErrorHandler onError { get; set; }
        public SessionSocketProxyCloseHandler onClose { get; set; }
#endregion
        protected ILogger logger { get; private set; } = null;

        public virtual void Start(Socket sck, ILogger lgr)
        {
            socket = sck ?? throw new ArgumentNullException(nameof(sck));
            logger = lgr ?? throw new ArgumentNullException(nameof(lgr));
            
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
        public abstract void Close();

        public abstract ValueTask CloseAsync();

        public abstract int Send(ReadOnlyMemory<byte> sendMemory);

        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}