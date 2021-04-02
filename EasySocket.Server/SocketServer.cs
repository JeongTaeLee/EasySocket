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
        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private List<IListener> _listeners = new List<IListener>();

        public SocketServerConfig config { get; private set; } = new SocketServerConfig();
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;
        
        protected override async ValueTask ProcessStart()
        {
            await StartListenersAsync();
        }

        protected override async ValueTask ProcessStop()
        {
            await StopListenersAsync();
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

        private void SocketSetting(Socket sck)
        {
            sck.LingerState = new LingerOption(true, 0);

            sck.SendBufferSize = config.sendBufferSize;
            sck.ReceiveBufferSize = config.recvBufferSize;

            sck.NoDelay = config.noDelay;

            if (0 < config.sendTimeout)
            {
                sck.SendTimeout = config.sendTimeout;
            }

            if (0 < config.recvTimeout)
            {
                sck.ReceiveBufferSize = config.recvTimeout;
            }
        }

        protected virtual async ValueTask OnSocketAcceptedFromListeners(IListener listener, Socket sck)
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

                tempSession
                    .SetLogger(loggerFactory.GetLogger(typeof(TSession)))
                    .SetSessionId(sessionId)
                    .SetOnStop(OnSessionStoppedFromSession)
                    .SetMsgFilter(msgFilterFactory.Get())
                    .SetSocket(sck);

                sessionConfigrator?.Invoke(tempSession);

                await tempSession.StartAsync().ConfigureAwait(false);

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
                        sessionContainer.CancelPreoccupancySessionId(sessionId);
                    }

                    sck?.SafeClose();
                }
                else
                {
                    sessionContainer.SetSession(sessionId, session);
                    
                }
            }
        }

        protected virtual void OnErrorOccurredFromListeners(IListener listener, Exception ex)
        {
            OnError(ex);
        }

        protected abstract IListener CreateListener();
    }
}