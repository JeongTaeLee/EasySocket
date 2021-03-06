using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.SocketProxys;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketSessionWorker : BaseSocketSessionWorker
    {
        protected override ISocketProxy CreateSocketProxy()
        {
            return new AsyncSocketProxy();
        }
    }
}