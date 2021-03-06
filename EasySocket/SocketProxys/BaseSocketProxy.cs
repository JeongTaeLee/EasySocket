using System;
using System.Net.Sockets;
using EasySocket.Workers;

namespace EasySocket.SocketProxys
{
    public abstract class BaseSocketProxy : ISocketProxy
    {
        public Socket socket { get; private set; } = null;

        public SessionSocketProxyReceiveHandler received { get; set; }
        
        public SessionSocketProxyErrorHandler error { get; set; }

        public virtual void Initialize(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            this.socket = null;
        }

        public virtual void Start()
        {
            if (socket == null)
            {
                throw new InvalidOperationException("Socket not set : Please check if the SocketProxy has been initialized.");
            }
        }

        public abstract void Stop();
    }
}