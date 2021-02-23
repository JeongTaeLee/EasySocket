namespace EasySocket.Behavior
{
    /// <summary>
    /// 서버 처리에 행동을 정의하는 핸들러 클래스입니다.
    /// </summary>
    public interface IServerBehavior
    {
        /// <summary>
        /// 세션이 연결된 후 호출됩니다.
        /// Accepted -> Create and configation session -> Call!..
        /// </summary>
        void OnSessionConnected();

        /// <summary>
        /// 새션의 연결이 종료되었을 때 호출됩니다.
        /// Socket Close -> session close process -> Call!
        /// </summary>
        void OnSessionDisconnected();
    }
}