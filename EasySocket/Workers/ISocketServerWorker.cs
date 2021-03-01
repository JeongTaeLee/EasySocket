using System.Collections.Generic;
using EasySocket.Behaviors;
using EasySocket.Listeners;

namespace EasySocket.Workers
{
    /// <summary>
    /// <see cref="EasySocket.Core.Listener.IListener"/>를 작동시키고
    /// 연결 요청이 온 세션을 생성, 삭제, 관리하는 클래스입니다.
    /// </summary>
    public interface ISocketServerWorker
    {
        /// <summary>
        /// 해당 서버의 설정 객체인 <see cref="EasySocket.Workers.ISocketServerWorkerConfig"> 입니다.
        /// </summary>
        /// <value></value>
        ISocketServerWorkerConfig config { get; }

        /// <summary>
        /// 해당 서버를 소유하는 <see cref="EasySocket.EasySocketService"/> 입니다.
        /// </summary>
        EasySocketService service { get; }

        /// <summary>
        /// 소켓의 연결을 수락하는 다수의 <see cref="EasySocket.Core.Listener.IListener"/> 입니다.
        /// </summary>
        IReadOnlyList<IListener> listeners { get; }

        /// <summary>
        /// 해당 서버의 이벤트를 처리하는 <see cref="EasySocket.Behaviors.IServerBehavior"/> 입니다. 
        /// </summary>
        IServerBehavior behavior { get; }

        /// <summary>
        /// 서버를 초기화 후 시작합니다.
        /// </summary>
        /// <param name="services">해당 서버를 소유하는 <see cref="EasySocket.EasySocketService"/> 입니다.</param>
        void Start(EasySocketService services);

        /// <summary>
        /// 해당 서버의 이벤트를 처리하는 <see cref="EasySocket.Behavior.IServerBehavior"/>를 설정합니다. 
        /// </summary>
        ISocketServerWorker SetServerBehavior(IServerBehavior behavior);

        /// <summary>
        /// 해당 서버의 설정 객체인 <see cref="EasySocket.Workers.ISocketServerWorkerConfig"/>를 설정합니다.
        /// </summary>
        ISocketServerWorker SetServerConfig(ISocketServerWorkerConfig config);
    }
}