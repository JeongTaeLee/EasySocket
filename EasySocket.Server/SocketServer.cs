using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using EasySocket.Common.Extensions;
using EasySocket.Server.Listeners;
using EasySocket.Common.Logging;

namespace EasySocket.Server
{
    public abstract class SocketServer<TServer, TSession, TPacket> : BaseServer<TServer, TSession, TPacket>
        where TServer : BaseServer<TServer, TSession, TPacket>
        where TSession : SocketSession<TSession, TPacket>
    {
        public override ServerState state => (ServerState)_state;


        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private List<IListener> _listeners = new List<IListener>();
        private int _state = (int)ServerState.None;

        public SocketServerConfig config { get; private set; } = new SocketServerConfig();
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;
        
        public override async ValueTask StartAsync()
        {
            try
            {
                int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Starting, (int)ServerState.None);
                if (prevState != (int)ServerState.None)
                {
                    throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
                }

                InternalInitialize();

                await ProcessStart().ConfigureAwait(false);

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
        
        public override async ValueTask StopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Stopping, (int)ServerState.Running);
            if (prevState != (int)ServerState.Running)
            {
                throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
            }

            await ProcessStop().ConfigureAwait(false);

            await StopListenersAsync();

            _state = (int)ServerState.Stopped;
        }

        protected override void InternalInitialize()
        {
            base.InternalInitialize();

            if (0 >= _listenerConfigs.Count)
            {
                throw new InvalidOperationException("At least one ListenerConfig is not set : Please call the \"AddListener\" Method and set it up.");
            }
        }

        public TServer AddListener(ListenerConfig lstnrCnfg)
        {
            _listenerConfigs.Add(lstnrCnfg);
            return this as TServer;
        }

        private async ValueTask StartListenersAsync()
        {
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