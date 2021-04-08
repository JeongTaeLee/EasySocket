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

        public ILoggerFactory loggerFactory { get; private set; } = null;
        public SocketConfig socketConfig { get; private set; } = new SocketConfig();
        public IMsgFilterFactory msgFilterFactory { get; private set; } = null;

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

        private void SocketSetting(Socket sck)
        {
            sck.LingerState = new LingerOption(true, 0);

            sck.SendBufferSize = socketConfig.sendBufferSize;
            sck.ReceiveBufferSize = socketConfig.recvBufferSize;

            sck.NoDelay = socketConfig.noDelay;

            if (0 < socketConfig.sendTimeout)
            {
                sck.SendTimeout = socketConfig.sendTimeout;
            }

            if (0 < socketConfig.recvTimeout)
            {
                sck.ReceiveBufferSize = socketConfig.recvTimeout;
            }
        }

        protected async ValueTask OnAcceptFromListener(IListener listener, Socket sck)
        {
            TSession session = null;
            string sessionId = string.Empty;

            try
            {
                if (state != ServerState.Running)
                {
                    throw new Exception("A socket connection was attempted with the server shut down.");
                }

                SocketSetting(sck);

                if (!sessionContainer.TryPreoccupancySessionId(out sessionId))
                {
                    throw new Exception("Unable to create Session Id.");
                }

                var msgFilter = msgFilterFactory.Get();
                if (msgFilter == null)
                {
                    throw new Exception("MsgFilterFactory.Get retunred null");
                }

                var tempSession = CreateSession();
                if (tempSession == null)
                {
                    throw new Exception("CreateSession retunred null");
                }

                sessionConfigrator?.Invoke(tempSession);

                var ssnPrmtr = new SessionParameter<TSession>(sessionId, msgFilter, OnStopFromSession, _sessionLogger);

                await tempSession.StartAsync(sck, ssnPrmtr)
                    .ConfigureAwait(false);

                // finally에서 오류 체크를 하기 위해 모든 작업이 성공적으로 끝난 후 대입해줍니다.
                session = tempSession;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                // 세션을 생성하지 못하면 연결이 실패한 것으로 관리합니다.
                if (session == null)
                {
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        sessionContainer.RemoveSession(sessionId);
                    }

                    sck?.SafeClose();
                }
                else
                {
                    sessionContainer.SetSession(sessionId, session);
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
                sessionContainer.RemoveSession(session.sessionId);
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

        public TServer SetSocketConfig(SocketConfig sckCnfg)
        {
            socketConfig = sckCnfg ?? throw new ArgumentNullException(nameof(sckCnfg));
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

        //
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