using System.Collections.Generic;
using EasySocket.Behaviors;
using EasySocket.Listeners;

namespace EasySocket.Workers
{
    /// <summary>
    /// <see cref="EasySocket.Core.Listener.IListener"/>를 작동시키고
    /// 연결 요청이 온 세션을 생성, 삭제, 관리하는 객체입니다.
    /// </summary>
    public interface IServerWorker
    {
        /// <summary>
        /// 소켓의 연결을 수락하는 다수의 <see cref="EasySocket.Core.Listener.IListener"/> 입니다.
        /// </summary>
        IReadOnlyList<IListener> listeners { get; }

        /// <summary>
        /// 해당 서버의 이벤트를 처리하는 <see cref="EasySocket.Behavior.IServerBehavior"/>를 설정합니다. 
        /// </summary>
        void SetServerBehavior(IServerBehavior behavior);
    }
}