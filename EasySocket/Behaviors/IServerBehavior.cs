using System;
using EasySocket.Workers;

namespace EasySocket.Behaviors
{
    /// <summary>
    /// 서버의 각종 이벤트를 구현하는 클래스입니다.
    /// </summary>
    public interface IServerBehavior
    {
        /// <summary>
        /// 세션이 연결된 후 호출됩니다.
        /// Accepted -> Create and configation session -> Call!..
        /// </summary>
        void OnSessionConnected(ISocketSessionWorker session);

        /// <summary>
        /// 새션의 연결이 종료되었을 때 호출됩니다.
        /// Socket Close -> session close process -> Call!
        /// </summary>
        void OnSessionDisconnected(ISocketSessionWorker session);

        /// <summary>
        /// 서버 내부에서 오류 발생 시 호출됩니다.
        /// </summary>
        void OnError(Exception ex);
    }
}