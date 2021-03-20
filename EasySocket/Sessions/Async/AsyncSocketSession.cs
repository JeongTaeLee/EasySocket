using EasySocket.SocketProxys;

namespace EasySocket.Sessions.Async
{
    public class AsyncSocketSession : BaseSocketSession
    {
        protected override ISocketProxy CreateSocketProxy()
        {
            return new AsyncSocketProxy();
        }
    }
}