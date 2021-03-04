using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.SocketProxys;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketSessionWorker : BaseSocketSessionWorker
    {
        public AsyncSocketSessionWorker(ISocketServerWorker server, AsyncSocketProxy socketProxy)
            : base(server, socketProxy)
        {
        }
    }
}