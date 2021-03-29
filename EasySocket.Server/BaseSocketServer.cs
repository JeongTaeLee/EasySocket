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
    public abstract class BaseSocketServer<TServer, TSession, TPacket> : IServer<TServer, TPacket>
        where TServer : BaseSocketServer<TServer, TSession, TPacket>
        where TSession : BaseSocketSession<TSession, TPacket>
    {
        public ServerState state => (ServerState)_state;

        private int _state = (int)ServerState.None;
        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private List<IListener> _listeners = new List<IListener>();

        protected ILogger logger { get; private set; } = null;

        public SocketServerConfig config { get; private set; } = new SocketServerConfig();
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;

        public IMsgFilterFactory<TPacket> msgFilterFactory { get; private set; } = null;
        public IServerBehavior<TPacket> behavior { get; private set; } = null;
        public Action<ISession<TPacket>> sessionConfigrator { get; private set; } = null;
        public ILoggerFactory loggerFactory { get; private set; } = null;

        public async Task StartAsync()
        {
            try
            {
                int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Starting, (int)ServerState.None);
                if (prevState != (int)ServerState.None)
                {
                    throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
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

        public async Task StopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Stopping, (int)ServerState.Running);
            if (prevState != (int)ServerState.Running)
            {
                throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
            }

            await ProcessStop();

            await StopListenersAsync().ConfigureAwait(false);

            _state = (int)ServerState.Stopped;
        }

        public TServer SetMsgFilterFactory(IMsgFilterFactory<TPacket> msgFltrFctr)
        {
            msgFilterFactory = msgFltrFctr ?? throw new ArgumentNullException(nameof(msgFltrFctr));
            return this as TServer;
        }

        public TServer SetServerBehavior(IServerBehavior<TPacket> bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as TServer;
        }

        public TServer SetSessionConfigrator(Action<ISession<TPacket>> ssnCnfgr)
        {
            sessionConfigrator = ssnCnfgr ?? throw new ArgumentNullException(nameof(ssnCnfgr));
            return this as TServer;
        }

        public TServer SetLoggerFactroy(ILoggerFactory lgrFctr)
        {
            loggerFactory = lgrFctr ?? throw new ArgumentNullException(nameof(lgrFctr));
            return this as TServer;
        }
        public TServer AddListener(ListenerConfig lstnrCnfg)
        {
            _listenerConfigs.Add(lstnrCnfg);
            return this as TServer;
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