using System;
using System.Collections.Generic;
using EasySocket.Behaviors;
using EasySocket.Listeners;

namespace EasySocket.Workers
{
    /// <summary>
    /// <see cref="IListener"/>를 작동시키고
    /// 연결 요청이 온 세션을 생성, 삭제, 관리하는 클래스입니다.
    /// </summary>
    public interface ISocketServerWorker
    {
        /// <summary>
        /// 해당 <see cref="ISocketServerWorker"/>의 설정 객체인 <see cref="ISocketServerWorkerConfig"/> 입니다.
        /// </summary>
        /// <value></value>
        ISocketServerWorkerConfig config { get; }

        /// <summary>
        /// 해당 <see cref="ISocketServerWorker"/>를 소유하는 <see cref="EasySocketService"/> 입니다.
        /// </summary>
        EasySocketService service { get; }

        /// <summary>
        /// 해당 <see cref="ISocketServerWorker"/>에서 실행하는 여러개의 <see cref="IListener"/> 설정 객체인 <see cref="ListenerConfig"/> 입니다.
        /// </summary>
        IReadOnlyList<ListenerConfig> listenerConfigs { get; }

        /// <summary>
        /// 해당 <see cref="ISocketServerWorker"/>의 이벤트를 처리하는 <see cref="IServerBehavior"/> 입니다. 
        /// </summary>
        IServerBehavior behavior { get; }

        /// <summary>
        /// <see cref="ISocketServerWorker"/>를 초기화 후 시작합니다.
        /// </summary>
        /// <param name="services">해당 서버를 소유하는 <see cref="EasySocketService"/> 입니다.</param>
        void Start(EasySocketService services);

        /// <summary>
        /// 해당 <see cref="ISocketServerWorker"/>에서 실행할 <see cref="IListener"/>의 설정 객체인 <see cref="ListenerConfig"/>를 추가합니다.
        /// </summary>
        /// <param name="listener">생성할 <see cref="IListener"/>의 설적 객체인<see cref="ListenerConfig"/> 입니다.</param>
        ISocketServerWorker AddListener(ListenerConfig listenerConfig);

        /// <summary>
        /// 해당 <see cref="ISocketServerWorker"/>의 이벤트를 처리하는 <see cref="IServerBehavior"/>를 설정합니다. 
        /// </summary>
        ISocketServerWorker SetServerBehavior(IServerBehavior behavior);

        /// <summary>
        /// 해당 <see cref="ISocketServerWorker"/>의 설정 객체인 <see cref="ISocketServerWorkerConfig"/>를 설정합니다.
        /// </summary>
        ISocketServerWorker SetServerConfig(ISocketServerWorkerConfig config);
    }
}