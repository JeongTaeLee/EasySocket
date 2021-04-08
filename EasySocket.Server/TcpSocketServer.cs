using System.Threading.Tasks;
using EasySocket.Server.Listeners;

namespace EasySocket.Server
{
    public class TcpSocketServer : SocketServer<TcpSocketServer, TcpSocketSession>
    {

        protected override IListener CreateListener()
        {
            return new TcpSocketListener();
        }

        protected override TcpSocketSession CreateSession()
        {
            return new TcpSocketSession();
        }
    }
}