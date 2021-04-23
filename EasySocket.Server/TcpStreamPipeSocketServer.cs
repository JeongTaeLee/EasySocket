using System.Threading.Tasks;
using EasySocket.Server.Listeners;

namespace EasySocket.Server
{
    public class TcpStreamPipeSocketServer : SocketServer<TcpStreamPipeSocketServer, TcpStreamPipeSocketSession>
    {
        protected override IListener CreateListener()
        {
            return new TcpSocketListener();
        }

        protected override TcpStreamPipeSocketSession CreateSession()
        {
            return new TcpStreamPipeSocketSession();
        }
    }
}