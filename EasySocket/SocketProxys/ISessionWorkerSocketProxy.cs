using System;
using System.Buffers;
using System.Net.Sockets;
using EasySocket.Workers;

namespace EasySocket.SocketProxys
{
    /// <summary>
    /// <see cref="EasySocket.SocketProxys.ISocketSessionWorkerSocketProxy"/>의 소켓에서 데이터를 받았을 시 호출되는 핸들러
    /// </summary>
    /// <param name="receiveCount">받은 데이터 길이(<see cref="byte"/>)</param>
    /// <returns>파싱한 데이터 길이(<see cref="byte"/>)</returns>
    public delegate int SessionSocketProxyReceiveHandler(ref ReadOnlySequence<byte> sequence);

    /// <summary>
    /// <see cref="System.Net.Sockets.Socket"/>을 관리하는 클래스 입니다.
    /// </summary>
    public interface ISocketSessionWorkerSocketProxy
    {
        /// <summary>
        /// <see cref="EasySocket.SocketProxys.ISocketSessionWorkerSocketProxy"/>이 관리하는 <see cref="System.Net.Sockets.Socket"/> 입니다.
        /// </summary>
        Socket socket { get; }

        /// <summary>
        /// <see cref="EasySocket.SocketProxys.ISocketSessionWorkerSocketProxy"/>의 소켓에서 데이터를 받았을 때 호출되는 콜백입니다.
        /// </summary>
        /// <value></value>
        SessionSocketProxyReceiveHandler received { get; set; }

        /// <summary>
        /// <see cref="EasySocket.SocketProxys.ISocketSessionWorkerSocketProxy"/>를 시작합니다. 
        /// </summary>
        void Start();
 
        /// <summary>
        /// <see cref="EasySocket.SocketProxys.ISocketSessionWorkerSocketProxy"/>를 중지합니다.
        /// </summary>
        void Stop();
    }
}