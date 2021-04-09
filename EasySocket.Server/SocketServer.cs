using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using EasySocket.Common.Extensions;
using EasySocket.Server.Listeners;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters.Factories;

namespace EasySocket.Server
{
    public abstract class SocketServer<TServer, TSession> : IServer
        where TServer : SocketServer<TServer, TSession>
        where TSession : SocketSession<TSession>
    {
        public ServerState state => (ServerState)_state;
        public int sessionCount => sessionContainer.count;

        private int _state = (int)ServerState.None;
        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private List<IListener> _listeners = new List<IListener>();
        private ILogger _sessionLogger = null;

        protected ISessionContainer<TSession> sessionContainer { get; private set; } = new GUIDSessionContainer<TSession>();
        protected ILogger logger { get; private set; } = null;

        public ISocketServerConfig socketServerConfig { get; private set; } = new SocketServerConfig();
        public IMsgFilterFactory msgFilterFactory { get; private set; } = null;
        public ILoggerFactory loggerFactory { get; private set; } = null;

        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;
        public Action<ISession> sessionConfigrator { get; private set; } = null;
        public Action<TServer, Exception> onError { get; private set; } = null;

        public async ValueTask StartAsync()
        {
            #region Check Members
            if (loggerFactory == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("LoggerFactory", "SetLoggerFactory");
            }

            logger = loggerFactory.GetLogger(GetType());
            if (logger == null)
            {
                throw new InvalidOperationException("Unable to get logger from LoggerFactory");
            }

            _sessionLogger = loggerFactory.GetLogger(typeof(TSession));
            if (_sessionLogger == null)
            {
                throw new InvalidOperationException("Unable to get session session logger from LoggerFactory");
            }

            if (msgFilterFactory == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("MsgFilterFactory", "SetMsgFilterFactory");
            }

            if (sessionConfigrator == null)
            {
                logger.MemberNotSetWarn("Session Configrator", "SetSessionConfigrator");
            }

            if (onError == null)
            {
                logger.MemberNotSetWarn("OnError", "SetOnError");
            }
            #endregion Check Members

            try
            {
                int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Starting, (int)ServerState.None);
                if (prevState != (int)ServerState.None)
                {
                    throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
                }

                await ProcessStart();
                await StartListenersAsync();

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
                throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
            }

            await StopListenersAsync();
            await StopAllSession();
            await ProcessStop();

            throw new NotImplementedException();
        }

        private async ValueTask StartListenersAsync()
        {
            if (0 >= _listenerConfigs.Count)
            {
                throw new InvalidOperationException("At least one ListenerConfig is not set : Please call the \"AddListener\" Method and set it up.");
            }

            List<Task> tasks = new List<Task>(_listenerConfigs.Count);
            foreach (var listenerConfig in _listenerConfigs)
            {
                var listener = CreateListener();
                listener.onAccept = OnAcceptFromListener;
                listener.onError = OnErrorFromListener;

                tasks.Add(listener.StartAsync(listenerConfig, loggerFactory.GetLogger(listener.GetType())));

                logger.DebugFormat("Started listener : {0}", listenerConfig.ToString());
            }

            await Task.WhenAll(tasks);
        }

        private async ValueTask StopListenersAsync()
        {
            if (0 >= _listeners.Count)
            {
                return;
            }

            List<Task> tasks = new List<Task>(_listenerConfigs.Count);
            foreach (var listener in _listeners)
            {
                tasks.Add(listener.StopAsync());
            }
            _listeners.Clear();

            await Task.WhenAll(tasks);
        }

        private async ValueTask StopAllSession()
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
                    OnError(ex);
                }
            }

            await Task.WhenAll(tasks);
        }

        private void SocketSetting(Socket sck)
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
                await session.StartAsync(sck, ssnPrmtr)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnError(ex);
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
        }

        protected void OnErrorFromListener(IListener listener, Exception ex)
        {
            OnError(ex);
        }

        protected void OnStopFromSession(TSession session)
        {
            try
            {
                sessionContainer.RemoveSession(session.id);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        protected void OnError(Exception ex)
        {
            onError?.Invoke(this as TServer, ex);
        }

        protected virtual ValueTask ProcessStart()
        {
            return new ValueTask();
        }

        protected virtual ValueTask ProcessStop()
        {
            return new ValueTask();
        }

        protected abstract IListener CreateListener();
        protected abstract TSession CreateSession();

        public TServer AddListener(ListenerConfig lstnrCnfg)
        {
            _listenerConfigs.Add(lstnrCnfg);
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

        public TServer SetSocketServerConfig(ISocketServerConfig sckServCnfg)
        {
            socketServerConfig = sckServCnfg.DeepClone();
            if (socketServerConfig == null)
            {
                throw new AbandonedMutexException(nameof(sckServCnfg));
            }

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
        
        public ISession GetSessionById(string ssnId) => sessionContainer.GetSession(ssnId);
    }
}