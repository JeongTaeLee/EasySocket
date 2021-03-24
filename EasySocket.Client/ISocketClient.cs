using System.Net.Sockets;

namespace EasySocket.Client
{
    public interface ISocketClient<TSocketClient> : IClient
        where TSocketClient : class, ISocketClient<TSocketClient>
    {
        SocketClientConfig config { get; }
        Socket socket { get; }
    }
}