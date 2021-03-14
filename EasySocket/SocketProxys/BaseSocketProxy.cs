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
        public SessionSocketProxyReceiveHandler received { get; set; }
        public SessionSocketProxyErrorHandler error { get; set; }
        public SessionSocketProxyCloseHandler close { get; set; }
#endregion

        protected ILogger logger { get; private set; } = null;

        public virtual void Start(Socket socket, ILogger logger)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.socket = socket;
            this.logger = logger;

            if (received == null)
            {
                logger.Warn("Received Handler is not set : Unable to receive events for socket receiving data.");
            }

            if (error == null)
            {
                logger.Warn("Error Handler is not set : Unable to receive events for error.");
            }
        }
        public abstract void Close();

        public abstract ValueTask CloseAsync();

        public abstract int Send(ReadOnlyMemory<byte> sendMemory);

        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}