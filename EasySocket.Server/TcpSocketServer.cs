using System.Threading.Tasks;
using EasySocket.Server.Listeners;

namespace EasySocket.Server
{
    public class TcpSocketServer : BaseSocketServer<TcpSocketServer, TcpSocketSession>
    {
        protected override IListener CreateListener()
        {
            return new TcpSocketListener();
        }

        protected override TcpSocketSession CreateSession()
        {
            return new TcpSocketSession();
        }

        protected override ValueTask InternalStart()
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask InternalStop()
        {
            return ValueTask.CompletedTask;
        }
    }
}