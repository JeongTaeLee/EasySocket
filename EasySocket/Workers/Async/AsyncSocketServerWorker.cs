using System.Net.Sockets;
using EasySocket.Listeners;
using EasySocket.SocketProxys;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketServerWorker: BaseSocketServerWorker<AsyncSocketSessionWorker>
    {
        protected override IListener CreateListener()
        {
            return new AsyncSocketListener(this);
        }

        protected override AsyncSocketSessionWorker CreateSession(Socket socket)
        {
            var socketProxy = new AsyncSocketProxy(socket);

            return new AsyncSocketSessionWorker(this, socketProxy);
        }
    }
}