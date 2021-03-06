using System.Net.Sockets;
using EasySocket.Listeners;
using EasySocket.SocketProxys;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketServerWorker: BaseSocketServerWorker<AsyncSocketSessionWorker>
    {
        protected override IListener CreateListener()
        {
            return new AsyncSocketListener();
        }

        protected override AsyncSocketSessionWorker CreateSession()
        {
            return new AsyncSocketSessionWorker();
        }
    }
}