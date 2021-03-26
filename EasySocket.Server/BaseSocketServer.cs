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
    public abstract class BaseSocketServer<TSocketServer, TSocketSession> : IServer<TSocketServer>
        where TSocketServer : BaseSocketServer<TSocketServer, TSocketSession>
        where TSocketSession : BaseSocketSession<TSocketSession>
    {
        public IServer.State state => (IServer.State)_state;
        public IMsgFilterFactory msgFilterFactory { get; private set; } = null;
        public IServerBehavior behavior { get; private set; } = null;
        public ILoggerFactory loggerFactory { get; private set; } = null;

        private int _state = (int)IServer.State.None;
        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private List<IListener> _listeners = new List<IListener>();

        protected SocketServerConfig config { get; private set; } = null;
        protected ILogger logger { get; private set; } = null;

        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;

        public async Task StartAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)IServer.State.Starting, (int)IServer.State.None);
            if (prevState != (int)IServer.State.None)
            {
                throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(IServer.State)prevState}");
            }

            if (msgFilterFactory == null)
            {
                throw new InvalidOperationException("MsgFilterFactory is not set : Please call the \"SetMsgFilterFactory\" Method and set it up.");
            }

            if (loggerFactory == null)
            {
                throw new InvalidOperationException("LoggerFactory is not set : Please call the \"SetLoggerFactory\" Method and set it up.");
            }

            logger = loggerFactory.GetLogger(GetType());
            if (logger == null)
            {
                throw new InvalidOperationException("Unable to get logger from LoggerFactory");
            }

            if (behavior == null)
            {
                logger.Warn("Server Behavior is not set. : Unable to receive events for the server. Please call the \"SetServerBehavior\" Method and set it up.");
            }

            await StartListenersAsync().ConfigureAwait(false);

            _state = (int)IServer.State.Running;
        }

        public async Task StopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)IServer.State.Stopping, (int)IServer.State.Running);
            if (prevState != (int)IServer.State.Running)
            {
                throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(IServer.State)prevState}");
            }

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

        public TSocketServer SetLoggerFactroy(ILoggerFactory lgrFctr)
        {
            loggerFactory = lgrFctr ?? throw new ArgumentNullException(nameof(lgrFctr));
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

                tasks.Add(listener.StartAsync(listenerConfig));

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

        protected virtual void OnSocketAcceptedFromListeners(IListener listener, Socket acptdSck)
        {
            TSocketSession session = null;

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
                    return;
                }

                var tempSession = CreateSession();
                if (tempSession == null)
                {
                    return;
                }
                tempSession.StartAsync(acptdSck).GetAwaiter().GetResult();

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
            }
        }

        protected virtual void OnErrorOccurredFromListeners(IListener listener, Exception ex)
        {

        }

        protected abstract ValueTask InternalStart();
        protected abstract ValueTask InternalStop();
        protected abstract TSocketSession CreateSession();
        protected abstract IListener CreateListener();
    }
}