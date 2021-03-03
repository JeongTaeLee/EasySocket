using System.Net.Sockets;
using EasySocket.Listeners;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketServerWorker : BaseSocketServerWorker
    {
        protected override IListener CreateListener()
        {
            return new AsyncSocketListener(this);
        }

        protected override ISocketSessionWorker CreateSession()
        {
            return new AsyncSocketSessionWorker(this);
        }
    }
}