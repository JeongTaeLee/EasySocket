using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasySocket.Test.Components
{
    // TODO - 테스트용 클래스. 추후 클라이언트 라이브러리에 정상적인 구조 및 새 로직으로 작성해서
    //        추가한 후 그 클래스로 사용해야 한다.
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
