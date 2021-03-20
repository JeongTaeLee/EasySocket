using EasySocket.Listeners;
using EasySocket.Sessions.Async;

namespace EasySocket.Servers.Async
{
    public class AsyncSocketServer : BaseSocketServer<AsyncSocketSession>
    {
        protected override IListener CreateListener()
        {
            return new AsyncSocketListener();
        }

        protected override AsyncSocketSession CreateSession()
        {
            return new AsyncSocketSession();
        }
    }
}