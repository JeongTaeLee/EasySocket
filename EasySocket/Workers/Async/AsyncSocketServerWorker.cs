using System.Net.Sockets;
using EasySocket.Listeners;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketServerWorker : BaseServerWorker
    {
        protected override IListener CreateListener()
        {
            return new AsyncSocketListener(this);
        }
    }
}