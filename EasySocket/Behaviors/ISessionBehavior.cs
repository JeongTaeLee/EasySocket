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
    }
}