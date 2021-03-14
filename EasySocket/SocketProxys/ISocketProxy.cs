using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Logging;

namespace EasySocket.SocketProxys
{
    /// <summary>
    /// <see cref="ISocketProxy"/>의 소켓에서 데이터를 받았을 시 호출되는 핸들러
    /// </summary>
    /// <param name="receiveCount">받은 데이터 길이(<see cref="byte"/>)</param>
    /// <returns>파싱한 데이터 길이(<see cref="byte"/>)</returns>
    public delegate long SessionSocketProxyReceiveHandler(ref ReadOnlySequence<byte> sequence);
    public delegate void SessionSocketProxyErrorHandler(Exception ex);
    public delegate void SessionSocketProxyCloseHandler();
    /// <summary>
    /// <see cref="System.Net.Sockets.Socket"/>을 관리하는 클래스 입니다.
    /// </summary>
    public interface ISocketProxy
    {
        /// <summary>
        /// <see cref="ISocketProxy"/>이 관리하는 <see cref="Socket"/> 입니다.
        /// </summary>
        Socket socket { get; }

        /// <summary>
        /// <see cref="ISocketProxy"/>의 소켓에서 데이터를 받았을 때 호출되는 콜백입니다.
        /// </summary>
        /// <value></value>
        SessionSocketProxyReceiveHandler received { get; set; }

        /// <summary>
        /// <see cref="ISocketProxy"/>에서 오류 발생 시 호출됩니다.
        /// </summary>
        SessionSocketProxyErrorHandler error { get; set; }

        /// <summary>
        /// <see cref="ISocketProxy"/> 에서 관리하는 소켓이 종료 요청을 보냈을 때 호출됩니다.
        /// </summary>
        SessionSocketProxyCloseHandler close { get; set; }

        /// <summary>
        /// <see cref="ISocketProxy"/>를 시작합니다. 
        /// </summary>
        void Start(Socket socket, ILogger logger);

        /// <summary>
        /// 동기 방식으로 <see cref="ISocketProxy"/>를 중지합니다.
        /// </summary>
        void Close();

        /// <summary>
        /// 비동기 방식으로 <see cref="ISocketProxy"/>를 중지합니다.
        /// </summary>
        ValueTask CloseAsync();

        /// <summary>
        /// 동기 방식으로 데이터를 전송합니다.
        /// </summary>
        int Send(ReadOnlyMemory<byte> sendMemory);

        /// <summary>
        /// 비동기 방식으로 데이터를 전송합니다.
        /// </summary>
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}