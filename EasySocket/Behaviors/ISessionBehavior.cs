using System;
using EasySocket.Workers;
using EasySocket.Protocols.MsgInfos;
using EasySocket.Protocols.Filters;

namespace EasySocket.Behaviors
{
    /// <summary>
    /// 세션의 각종 이벤트를 구현하는 클래스입니다.
    /// </summary>
    public interface ISessionBehavior
    {
        /// <summary>
        /// 세션의 모든 시작 처리들이 끝난 후 마지막에 호출됩니다.
        /// Accepted -> Create and configation session 
        ///  -> IServerBehavior.OnSessionConnected -> Start Receive -> Call!
        /// </summary>
        void OnStarted();

        /// <summary>
        /// 세션의 모든 종료 처리들이 끝난 후 마지막에 호출됩니다.
        /// Socket Close -> session close process 
        ///  -> IServerBehavior.OnSessionConnected -> Call! 
        /// </summary>
        void OnClosed();

        /// <summary>
        /// 데이터를 수신한 후 <see cref="IMsgFilter"/>에서 <see cref="IMsgInfo"/>로 변환 후 호출됩니다.
        /// </summary>
        /// <param name="msg">변환 된 <see cref="IMsgInfo"/> 입니다.</param>
        void OnReceived(IMsgInfo msg);

        /// <summary>
        /// <see cref="ISocketSessionWorker"/> 내부에서 오류 발생 시 호출됩니다.
        /// </summary>
        void OnError(Exception ex);
    }
}