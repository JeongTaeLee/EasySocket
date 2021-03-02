using System;
using System.Net.Sockets;
using EasySocket.Workers;

namespace EasySocket.SocketProxys
{
    public abstract class BaseSocketSessionSocketProxy : ISocketSessionWorkerSocketProxy
    {
        public Socket socket { get; private set; } = null;

        public SessionSocketProxyReceiveHandler received { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket">해당 객체가 관리하는 <see cref="System.Net.Sockets.Socket"/></param>
        public BaseSocketSessionSocketProxy(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            this.socket = socket;
        }
        
        public abstract void Start();
        public abstract void Stop();
    }
}