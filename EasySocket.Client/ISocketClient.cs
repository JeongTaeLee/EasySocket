using System.Net.Sockets;

namespace EasySocket.Client
{
    public interface ISocketClient : IClient
    {
        Socket socket { get; }
        SocketClientConfig config { get; }

        void Start();
    }
}