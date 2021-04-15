using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasySocket.Test.Components
{
    public sealed class Connector
    {
        private readonly Socket _socket;
        private readonly EndPoint _endPoint;

        public Connector(EndPoint endPoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _endPoint = endPoint;
        }

        public async Task ConnectAsync()
        {
            await _socket.ConnectAsync(_endPoint);
        }

        public ValueTask CloseAsync()
        {
            _socket.Close();
            return ValueTask.CompletedTask;
        }

        public async Task SendStringAsync(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var buffer = new ArraySegment<byte>(bytes);

            await _socket.SendAsync(buffer, SocketFlags.None);
        }
    }
}
