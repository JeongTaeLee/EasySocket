using System.Net.Sockets;

namespace EasySocket.Client
{
    public interface ISocketClient<TSocketClient> : IClient<TSocketClient>
        where TSocketClient : class, ISocketClient<TSocketClient>
    {
        Socket socket { get; }
        SocketClientConfig config { get; }

        void Start();

        public TSocketClient SetSocketClientConfig(SocketClientConfig cnfg);
    }
}