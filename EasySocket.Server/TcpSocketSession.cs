using System.Threading.Tasks;

namespace EasySocket.Server
{
    public class TcpSocketSession : BaseSocketSession<TcpSocketSession>
    {
        protected override ValueTask InternalStart()
        {
            throw new System.NotImplementedException();
        }

        protected override ValueTask InternalStop()
        {
            throw new System.NotImplementedException();
        }
    }
}