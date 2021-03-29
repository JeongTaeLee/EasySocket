using System.Threading.Tasks;
using EasySocket.Server.Listeners;

namespace EasySocket.Server
{
    public class TcpSocketServer<TPacket> : SocketServer<TcpSocketServer<TPacket>, TcpSocketSession<TPacket>, TPacket>
    {
        protected override IListener CreateListener()
        {
            return new TcpSocketListener();
        }

        protected override TcpSocketSession<TPacket> CreateSession()
        {
            return new TcpSocketSession<TPacket>();
        }

        protected override ValueTask ProcessStart()
        {
            return new ValueTask();
        }

        protected override ValueTask ProcessStop()
        {
            return new ValueTask();
        }
    }
}