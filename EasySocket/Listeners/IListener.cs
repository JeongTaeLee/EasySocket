using System;
using System.Net.Sockets;
using EasySocket.Workers;

namespace EasySocket.Listeners
{
    public delegate void ListenerAcceptHandler(IListener listener, Socket socket);
    public delegate void ListenerErrorHandler(IListener listener, Exception ex);

    public interface IListener
    {
        /// <summary>
        /// 리스너 설정 객체입니다.
        /// </summary>
        ListenerConfig config { get; }

        /// <summary>
        /// 리스너에 새로운 소켓이 Accept 되었을 때 호출되는 콜백입니다.
        /// </summary>
        ListenerAcceptHandler accepted {get; set;}

        /// <summary>
        /// 리스너 내부에서 오류 발생시 호출되는 콜백입니다.
        /// </summary>
        ListenerErrorHandler error {get; set;}

        /// <summary>
        /// 리스너를 시작합니다.
        /// </summary>
        /// <param name="config">해당 리스너를 구성할 객체입니다.</param>   
        void Start(ListenerConfig config);

        /// <summary>
        /// 리스너를 종료합니다.
        /// </summary>
        void Close();
    }
}