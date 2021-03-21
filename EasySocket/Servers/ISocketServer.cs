using System.Collections.Generic;
using System.Threading.Tasks;
using EasySocket.Behaviors;
using EasySocket.Listeners;
using EasySocket.Protocols.Filters;
using EasySocket.Protocols.Filters.Factories;

namespace EasySocket.Servers
{
    /// <summary>
    /// <see cref="IListener"/>를 작동시키고
    /// 연결 요청이 온 세션을 생성, 삭제, 관리하는 클래스입니다.
    /// </summary>
    public interface ISocketServer
    {
        public enum State
        {
            None = 0,
            Running,
            Closed
        }

        /// <summary>
        /// 해당 <see cref="ISocketServer"/>의 설정 객체인 <see cref="ISocketServerConfig"/> 입니다.
        /// </summary>
        /// <value></value>
        ISocketServerConfig config { get; }

        /// <summary>
        /// 해당 <see cref="ISocketServer"/>를 소유하는 <see cref="EasySocketService"/> 입니다.
        /// </summary>
        EasySocketService service { get; }

        /// <summary>
        /// 수신한 데이터를 필터링하는 <see cref="IMsgFilter"/>를 생성하는 <see cref="IMsgFilter"/> 입니다.
        /// </summary>
        IMsgFilterFactory msgFilterFactory { get; }

        /// <summary>
        /// 해당 <see cref="ISocketServer"/>에서 실행하는 여러개의 <see cref="IListener"/> 설정 객체인 <see cref="ListenerConfig"/> 입니다.
        /// </summary>
        IReadOnlyList<ListenerConfig> listenerConfigs { get; }

        /// <summary>
        /// <see cref="ISocketServer"/>의 상태를 나타내는 Flag 입니다.
        /// </summary>
        State state { get; }

        /// <summary>
        /// 해당 <see cref="ISocketServer"/>의 이벤트를 처리하는 <see cref="IServerBehavior"/> 입니다. 
        /// </summary>
        IServerBehavior behavior { get; }

        /// <summary>
        /// <see cref="ISocketServer"/>를 동기적으로 초기화 후 시작합니다.
        /// </summary>
        /// <param name="services">해당 서버를 소유하는 <see cref="EasySocketService"/> 입니다.</param>
        void Start(EasySocketService services);

        /// <summary>
        /// <see cref ="ISocketServer"/>를 동기적으로 종료합니다.
        /// </summary>
        void Stop();

        /// <summary>
        /// <see cref ="ISocketServer"/>를 비동기적으로 종료합니다.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 해당 <see cref="ISocketServer"/>에서 실행할 <see cref="IListener"/>의 설정 객체인 <see cref="ListenerConfig"/>를 추가합니다.
        /// </summary>
        /// <param name="listener">생성할 <see cref="IListener"/>의 설적 객체인<see cref="ListenerConfig"/> 입니다.</param>
        ISocketServer AddListener(ListenerConfig listenerConfig);

        /// <summary>
        /// 해당 <see cref="ISocketServer"/>의 이벤트를 처리하는 <see cref="IServerBehavior"/>를 설정합니다. 
        /// </summary>
        ISocketServer SetServerBehavior(IServerBehavior behavior);

        /// <summary>
        /// 해당 <see cref="ISocketServer"/>의 설정 객체인 <see cref="ISocketServerConfig"/>를 설정합니다.
        /// </summary>
        ISocketServer SetServerConfig(ISocketServerConfig config);

        /// <summary>
        /// 수신한 데이터를 필터링하는 <see cref="Protocols.Filters.IMsgFilter"/>를 생성하는 <see cref="Protocols.Filters.IMsgFilter"/>를 설정합니다.
        /// </summary>
        ISocketServer SetMsgFilterFactory(IMsgFilterFactory msgFilterFactory);
    }
}