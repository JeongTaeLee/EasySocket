using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using EasySocket.Common.Logging;
using EasySocket.Server.Listeners;
using EasySocket.Common.Extensions;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Common.Protocols.MsgFilters;
using System.Collections.Concurrent;

namespace EasySocket.Server
{
    public abstract class SocketServer<TServer, TSession> : IServer
        where TServer : SocketServer<TServer, TSession>
        where TSession : SocketSession<TSession>
    {
        /// <summary>
        /// 서버의 상태(<see cref="ServerState"/>)를 반환하는 Property 입니다.
        /// </summary>
        public ServerState state => (ServerState)_state;

        /// <summary>
        /// 서버에 연결된 Session 개수 입니다.
        /// </summary>
        public int sessionCount => sessionContainer.count;

        private int _state = (int)ServerState.None;
        private ConcurrentDictionary<int, (ListenerConfig, IListener)> _listenerDict = new ConcurrentDictionary<int, (ListenerConfig, IListener)>();
        private ILogger _sessionLogger = null;
        private ILogger _listenerLogger = null;

        protected ISessionContainer<TSession> sessionContainer { get; private set; } = new GUIDSessionContainer<TSession>();
        protected ILogger logger { get; private set; } = null;

        /// <summary>
        /// 서버의 설정 객체입니다.
        /// </summary>
        public SocketServerConfig socketServerConfig { get; private set; } = new SocketServerConfig();

        /// <summary>
        /// 메세지를 필터링하는 <see cref="IMsgFilter"/> 파생 클래스를 생성하는 객체입니다.
        /// <see cref="SocketServer{TServer, TSession}.SetMsgFilterFactory(IMsgFilterFactory)"/>로 초기화 하며,
        /// Null 일 수 없습니다.
        /// </summary>
        public IMsgFilterFactory msgFilterFactory { get; private set; } = null;

        /// <summary>
        /// 로그를 출력하는 <see cref="ILogger"/> 파생 클래스를 생성하는 객체입니다.
        /// <see cref="SocketServer{TServer, TSession}.SetLoggerFactory(ILoggerFactory)"/>로 초기화 하며,
        /// Null 일 수 없습니다.
        /// </summary>
        public ILoggerFactory loggerFactory { get; private set; } = null;

        /// <summary>
        /// 서버에 새로운 세션이 연결 되었을때 연결된 세션을 설정하는 콜백 함수 입니다. 
        /// </summary>
        public Action<ISession> sessionConfigrator { get; private set; } = null;

        /// <summary>
        /// 서버 내부 로직에서 예외가 발생 했을 때 호출되는 콜백 함수 입니다. 
        /// </summary>
        public Action<TServer, Exception> onError { get; private set; } = null;

        public ValueTask StartAsync(ListenerConfig listenerCnfg)
        {
            return StartAsync(new List<ListenerConfig>() { listenerCnfg });
        }

        public async ValueTask StartAsync(List<ListenerConfig> listenerCnfgs)
        {
            if (state == ServerState.Stopped)
            {
                throw ExceptionExtensions.TerminatedObjectIOE("Server");
            }

            if (loggerFactory == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("LoggerFactory", "SetLoggerFactory");
            }

            logger = loggerFactory.GetLogger(typeof(TServer));
            if (logger == null)
            {
                throw new InvalidOperationException("Unable to get logger from LoggerFactory");
            }

            _sessionLogger = loggerFactory.GetLogger(typeof(TSession));
            if (_sessionLogger == null)
            {
                throw new InvalidOperationException("Unable to get session logger from LoggerFactory");
            }

            _listenerLogger = loggerFactory.GetLogger("Listener");
            if (_listenerLogger == null)
            {
                throw new InvalidOperationException("Unable to get listener logger from LoggerFactory");
            }

            if (msgFilterFactory == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("MsgFilterFactory", "SetMsgFilterFactory");
            }

            if (sessionConfigrator == null)
            {
                logger.MemberNotSetUseMethodWarn("Session Configrator", "SetSessionConfigrator");
            }

            if (onError == null)
            {
                logger.MemberNotSetUseMethodWarn("OnError", "SetOnError");
            }

            int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Starting, (int)ServerState.None);
            if (prevState != (int)ServerState.None)
            {
                throw ExceptionExtensions.CantStartObjectIOE("Server", (ServerState)prevState);
            }

            try
            {
                await InternalStartAsync();

                if (0 < listenerCnfgs.Count)
                {
                    await StartListenersAsync(listenerCnfgs);
                }

                _state = (int)ServerState.Running;
            }
            finally
            {
                if (_state != (int)ServerState.Running)
                {
                    _state = (int)ServerState.None;
                }
            }
        }

        public async ValueTask StopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Stopping, (int)ServerState.Running);
            if (prevState != (int)ServerState.Running)
            {
                throw ExceptionExtensions.CantStopObjectIOE("Server", (ServerState)prevState);
            }

            await StopAllListenerAsync();
            await StopAllSessionAsync();
            await InternalStopAsync();

            _state = (int)ServerState.Stopped;
        }

        /// <summary>
        /// 리스너에서 새 <see cref="Socket"/>이 연결되 었을 때 호출되는 콜백
        /// </summary>
        /// <param name="listener">호출한 <see cref="IListener"/>.</param>
        /// <param name="sck">연결된 <see cref="Socket"/>.</param>
        protected async ValueTask OnAcceptFromListener(IListener listener, Socket sck)
        {
            TSession session = null;
            string sessionId = string.Empty;

            try
            {
                // 서버 상태 체크.
                if (state != ServerState.Running)
                {
                    throw new Exception("A socket connection was attempted with the server shut down.");
                }

                if (sessionCount >= socketServerConfig.maxConnection)
                {
                    return;
                }

                // 소켓 상태 설정
                // TODO : 해당 로직 적절한 공간으로 이동.
                SocketSetting(sck);

                // 세션이 시작 전 새로운 세션 아이디를 받아둔다(해당 아이디는 예약된다.)
                if (!sessionContainer.TryPreoccupancySessionId(out sessionId))
                {
                    throw new Exception("Unable to create Session Id.");
                }

                // MsgFilter 가져오기
                var msgFilter = msgFilterFactory.Get();
                if (msgFilter == null)
                {
                    throw new Exception("MsgFilterFactory.Get retunred null");
                }

                // session 생성
                session = CreateSession();
                if (session == null)
                {
                    throw new Exception("CreateSession retunred null");
                }

                // 사용자 측 세션 설정 호출
                sessionConfigrator?.Invoke(session);

                // 세션 생성 시 필요한 데이터 설정.
                var ssnPrmtr = new SessionParameter<TSession>(sessionId, msgFilter, OnStopFromSession, _sessionLogger);

                // 설정 완료 후 생성한 ID로 컨테이너 등록.
                sessionContainer.SetSession(sessionId, session);


                // NOTE : 정상적인 플로우를 위해 세션 시작과 관련된 모든 작업은 마치고 session을 시작해야한다.
                await session.StartAsync(ssnPrmtr, sck)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ProcessError(ex);
            }
            finally
            {
                // 세션이 시작되지 못했다면 소켓을 중단.
                if (session == null || session.state != SessionState.Running)
                {
                    sck.SafeClose();

                    // 세션 등록 취소
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        sessionContainer.RemoveSession(sessionId);
                    }
                }
            }

            void SocketSetting(Socket sck)
            {
                sck.LingerState = new LingerOption(true, 0);

                sck.SendBufferSize = socketServerConfig.sendBufferSize;
                sck.ReceiveBufferSize = socketServerConfig.recvBufferSize;

                sck.NoDelay = socketServerConfig.noDelay;

                if (0 < socketServerConfig.sendTimeout)
                {
                    sck.SendTimeout = socketServerConfig.sendTimeout;
                }

                if (0 < socketServerConfig.recvTimeout)
                {
                    sck.ReceiveBufferSize = socketServerConfig.recvTimeout;
                }
            }
        }

        /// <summary>
        /// 리스너에서 에외 발생시 호출되는 콜백.
        /// </summary>
        /// <param name="listener">호출한 <see cref="IListener"/>.</param>
        /// <param name="ex">발생한 예외.</param>
        protected void OnErrorFromListener(IListener listener, Exception ex)
        {
            ProcessError(ex);
        }

        /// <summary>
        /// 세션이 종료될 때 최종적으로 호출듸는 콜백.
        /// </summary>
        /// <param name="session">종료된 세션.</param>
        protected void OnStopFromSession(TSession session)
        {
            try
            {
                sessionContainer.RemoveSession(session.id);
            }
            catch (Exception ex)
            {
                ProcessError(ex);
            }
        }

        /// <summary>
        /// 서버 내부에서 예외 발생시 호출되는 콜백
        /// </summary>
        /// <param name="ex">발샹한 예외</param>
        protected void ProcessError(Exception ex)
        {
            onError?.Invoke(this as TServer, ex);
        }

        /// <summary>
        /// 서버가 시작될때 호출되는 함수, <see cref="SocketServer{TServer, TSession}"/> 파생 클래스에서 재정의 합니다.
        /// </summary>
        protected virtual ValueTask InternalStartAsync() { return new ValueTask(); }

        /// <summary>
        /// 서버가 종료될때 호출되는 함수, <see cref="SocketServer{TServer, TSession}"/> 파생 클래스에서 재정의 합니다.
        /// </summary>
        protected virtual ValueTask InternalStopAsync() { return new ValueTask(); }

        /// <summary>
        /// 서버에서 사용할 리스너를 생성하는 함수 <see cref="SocketServer{TServer, TSession}"/> 파생 클래스에서 재정의 합니다.
        /// </summary>
        protected abstract IListener CreateListener();
        
        /// <summary>
        /// 서버에서 사용할 세션을 생성하는 함수 <see cref="SocketServer{TServer, TSession}"/> 파생 클래스에서 재정의 합니다.
        /// </summary>
        protected abstract TSession CreateSession();

        /// <summary>
        /// 새 리스너를 작동합니다.
        /// </summary>
        /// <param name="listenerCnfg">작동할 리스너의 설정 객체입니다.</param>
        public async ValueTask StartListenerAsync(ListenerConfig listenerCnfg)
        {
            if (!_listenerDict.TryAdd(listenerCnfg.port, default))
            {
                throw new InvalidOperationException($"The port is already open : the port({listenerCnfg.port}) number cannot be duplicated.");
            }

            try
            {
                var listener = CreateListener();

                listener.onAccept = OnAcceptFromListener;
                listener.onError = OnErrorFromListener;
        
                await listener.StartAsync(listenerCnfg, _listenerLogger);

                _listenerDict[listenerCnfg.port] = (listenerCnfg, listener);
            }
            catch (System.Exception)
            {
                _listenerDict.TryRemove(listenerCnfg.port, out var _);

                throw;
            }
        }

        /// <summary>
        /// 다수의 새 리스너를 작동합니다.
        /// </summary>
        /// <param name="listenerCnfgs">작동할 리스너의 설정 객체입니다.</param>
        public async ValueTask StartListenersAsync(List<ListenerConfig> listenerCnfgs)
        {
            if (listenerCnfgs == null)
            {
                throw new ArgumentNullException(nameof(listenerCnfgs));
            }

            if (0 >= listenerCnfgs.Count)
            {
                throw new ArgumentException($"{nameof(listenerCnfgs)} must contain at least one ListenerConfig");
            }

            foreach (var cnfg in listenerCnfgs)
            {
                await StartListenerAsync(cnfg);

                logger.InfoFormat("Started listener : {0}", cnfg.ToString());
            }
        }

        /// <summary>
        /// 인자의 포트로 작동중인 리스너를 중지합니다.
        /// </summary>
        /// <param name="port">작동중인 리스너의 포트</param>
        public async ValueTask StopListenerAsync(int port)
        {
            if (!_listenerDict.TryRemove(port, out var listenerPair))
            {
                throw new ArgumentNullException($"Listener(Port {port}) did not start.");
            }

            await listenerPair.Item2.StopAsync();
        }

        /// <summary>
        /// 모든 리스너를 중지합니다.
        /// </summary>
        public async ValueTask StopAllListenerAsync()
        {
            if (0 >= _listenerDict.Count)
            {
                return;
            }

            var listenerPairIter = _listenerDict.Values.GetEnumerator();
            while (listenerPairIter.MoveNext())
            {
                var curListenerPair = listenerPairIter.Current;
                await StopListenerAsync(curListenerPair.Item1.port);
            }
        }

        /// <summary>
        /// 모든 세션과의 연결을 종료합니다.
        /// </summary>
        public async ValueTask StopAllSessionAsync()
        {
            var iter = sessionContainer.GetSessionEnumerator();

            var tasks = new List<Task>();
            while (iter.MoveNext())
            {
                try
                {
                    var session = iter.Current as TSession;
                    tasks.Add(session.StopAsync().AsTask());
                }
                catch (Exception ex)
                {
                    ProcessError(ex);
                }
            }

            await Task.WhenAll(tasks);
        }

        #region Setter / Getter
        public TServer SetSocketServerConfig(SocketServerConfig sckServCnfg)
        {
            socketServerConfig = sckServCnfg?.DeepClone() ?? throw new ArgumentNullException(nameof(sckServCnfg));
            return this as TServer;
        }

        public TServer SetMsgFilterFactory(IMsgFilterFactory msgFltrFctry)
        {
            msgFilterFactory = msgFltrFctry ?? throw new ArgumentNullException(nameof(msgFltrFctry));
            return this as TServer;
        }

        public TServer SetLoggerFactory(ILoggerFactory lgrFctry)
        {
            loggerFactory = lgrFctry ?? throw new ArgumentNullException(nameof(lgrFctry));
            return this as TServer;
        }

        public TServer SetSessionConfigrator(Action<ISession> ssnCnfgr)
        {
            sessionConfigrator = ssnCnfgr ?? throw new ArgumentNullException(nameof(ssnCnfgr));
            return this as TServer;
        }

        public TServer SetOnError(Action<TServer, Exception> onErr)
        {
            onError = onErr ?? throw new ArgumentNullException(nameof(onErr));
            return this as TServer;
        }

        public ISession GetSessionById(string ssnId)
        {
            return sessionContainer.GetSession(ssnId);
        }
        #endregion
    }
}