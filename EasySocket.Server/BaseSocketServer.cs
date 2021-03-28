using System;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using EasySocket.Common.Logging;
using EasySocket.Server.Listeners;
using EasySocket.Common.Extensions;
using EasySocket.Common.Protocols.MsgFilters.Factories;

namespace EasySocket.Server
{
    public abstract class BaseSocketServer<TSocketServer, TSession> : IServer<TSocketServer>
        where TSocketServer : BaseSocketServer<TSocketServer, TSession>
        where TSession : BaseSocketSession<TSession>
    {
        public IServer.State state => (IServer.State)_state;
        public IMsgFilterFactory msgFilterFactory { get; private set; } = null;
        public IServerBehavior behavior { get; private set; } = null;
        public ILoggerFactory loggerFactory { get; private set; } = null;
        public Action<ISession> sessionConfigrator { get; private set; } = null;

        private int _state = (int)IServer.State.None;
        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private List<IListener> _listeners = new List<IListener>();

        protected ILogger logger { get; private set; } = null;

        public SocketServerConfig config { get; private set; } = new SocketServerConfig();
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;


        public async Task StartAsync()
        {
            try
            {

                int prevState = Interlocked.CompareExchange(ref _state, (int)IServer.State.Starting, (int)IServer.State.None);
                if (prevState != (int)IServer.State.None)
                {
                    throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(IServer.State)prevState}");
                }

                if (msgFilterFactory == null)
                {
                    throw ExceptionExtensions.MemberNotSetIOE("MsgFilterFactory", "SetMsgFilterFactory");
                }

                if (loggerFactory == null)
                {
                    throw ExceptionExtensions.MemberNotSetIOE("LoggerFactory", "SetLoggerFactory");
                }

                logger = loggerFactory.GetLogger(GetType());
                if (logger == null)
                {
                    throw new InvalidOperationException("Unable to get logger from LoggerFactory");
                }

                if (behavior == null)
                {
                    logger.MemberNotSetWarn("Server Behavior", "SetServerBehavior");
                }

                if (sessionConfigrator == null)
                {
                    logger.MemberNotSetWarn("Session Configrator", "SetSessionConfigrator");
                }

                await ProcessStart();

                await StartListenersAsync().ConfigureAwait(false);

                _state = (int)IServer.State.Running;
            }
            finally
            {
                if (_state != (int)IServer.State.Running)
                {
                    _state = (int)IServer.State.None;
                }
            }
        }

        public async Task StopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)IServer.State.Stopping, (int)IServer.State.Running);
            if (prevState != (int)IServer.State.Running)
            {
                throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(IServer.State)prevState}");
            }

            await ProcessStop();

            await StopListenersAsync().ConfigureAwait(false);

            _state = (int)IServer.State.Stopped;
        }

        public TSocketServer SetMsgFilterFactory(IMsgFilterFactory msgFltrFctr)
        {
            msgFilterFactory = msgFltrFctr ?? throw new ArgumentNullException(nameof(msgFltrFctr));
            return this as TSocketServer;
        }

        public TSocketServer SetServerBehavior(IServerBehavior bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as TSocketServer;
        }

        public TSocketServer SetSessionConfigrator(Action<ISession> ssnCnfgr)
        {
            sessionConfigrator = ssnCnfgr ?? throw new ArgumentNullException(nameof(ssnCnfgr));
            return this as TSocketServer;
        }

        public TSocketServer SetLoggerFactroy(ILoggerFactory lgrFctr)
        {
            loggerFactory = lgrFctr ?? throw new ArgumentNullException(nameof(lgrFctr));
            return this as TSocketServer;
        }
        public TSocketServer AddListener(ListenerConfig lstnrCnfg)
        {
            _listenerConfigs.Add(lstnrCnfg);
            return this as TSocketServer;
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
                listener.onAccept = OnSocketAcceptedFromListeners;
                listener.onError = OnErrorOccurredFromListeners;

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

        protected virtual async ValueTask OnSocketAcceptedFromListeners(IListener listener, Socket acptdSck)
        {
            TSession session = null;

            try
            {
                acptdSck.LingerState = new LingerOption(true, 0);

                acptdSck.SendBufferSize = config.sendBufferSize;
                acptdSck.ReceiveBufferSize = config.recvBufferSize;

                if (0 < config.sendTimeout)
                {
                    acptdSck.SendTimeout = config.sendTimeout;
                }

                if (0 < config.recvTimeout)
                {
                    acptdSck.ReceiveTimeout = config.recvTimeout;
                }

                acptdSck.NoDelay = config.noDelay;

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

                sessionConfigrator?.Invoke(tempSession
                    .SetOnStop(OnSessionStopFromSession)
                    .SetMsgFilter(msgFilterFactory.Get())
                    .SetLogger(loggerFactory.GetLogger(typeof(TSession))));

                await tempSession.StartAsync(acptdSck).ConfigureAwait(false);

                // finally에서 오류 체크를 하기 위해 모든 작업이 성공적으로 끝난 후 대입해줍니다.
                session = tempSession;
            }
            catch (Exception ex)
            {
                behavior?.OnError(this, ex);
            }
            finally
            {
                // 세션을 생성하지 못하면 연결이 실패한 것으로 관리합니다.
                if (session == null)
                {
                    acptdSck?.SafeClose();
                }
                else
                {
                    behavior?.OnSessionConnected(this, session);
                }
            }
        }

        protected virtual void OnErrorOccurredFromListeners(IListener listener, Exception ex)
        {
            behavior?.OnError(this, ex);
        }

        protected virtual void OnSessionStopFromSession(TSession session)
        {
            behavior?.OnSessionDisconnected(this, session);
        }

        protected abstract ValueTask ProcessStart();
        protected abstract ValueTask ProcessStop();
        protected abstract TSession CreateSession();
        protected abstract IListener CreateListener();

    }
}