using System.Threading.Tasks;
using EasySocket.Server.Listeners;

namespace EasySocket.Server
{
    public class TcpSocketServer<TPacket> : SocketServer<TcpSocketServer<TPacket>, TcpSocketSession<TPacket>, TPacket>
    {

        protected async override ValueTask ProcessStart()
        {
            // TODO @jeongtae.lee : 예시임 나중에 지우기.
            await base.ProcessStart();
        }


        protected async override ValueTask ProcessStop()
        {
            // TODO @jeongtae.lee : 예시임 나중에 지우기.
            await base.ProcessStop();
        }

        protected override IListener CreateListener()
        {
            return new TcpSocketListener();
        }

        protected override TcpSocketSession<TPacket> CreateSession()
        {
            return new TcpSocketSession<TPacket>();
        }

    }
}