using System;
using System.Collections.Generic;
using System.Net.Sockets;
using EasySocket.Behaviors;
using EasySocket.Listeners;
using EasySocket.Logging;
using EasySocket.Protocols.Filters.Factories;

namespace EasySocket.Workers
{
    public abstract class BaseSocketServerWorker<TSession> : ISocketServerWorker
        where TSession : BaseSocketSessionWorker
    {
#region ISocketServerWorker Field 
        public ISocketServerWorkerConfig config { get; private set; } = new SocketServerWorkerConfig();
        public EasySocketService service { get; private set; } = null;
        public IMsgFilterFactory msgFilterFactory { get; private set; } = null;
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;
        public IServerBehavior behavior { get; private set; } = null;
#endregion ISocketServerWorker Field

        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private IReadOnlyList<IListener> _listeners = null;
        
        protected ILogger logger { get; private set; } = null;

#region ISocketServerWorker Method

        public void Start(EasySocketService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (0 >= _listenerConfigs.Count)
            {
                throw new InvalidOperationException("At least one ListenerConfig is not set : Please call the \"AddListener\" Method and set it up.");
            }

            if (msgFilterFactory == null)
            {
                throw new InvalidOperationException("MsgFilterFactroy not set: Please call the \"SetMsgFilterFactory\" Method and set it up.");
            }
   
            this.service = service;
            this.logger = service.loggerFactroy.GetLogger(GetType());

            if (behavior == null)
            {
                logger.Warn("Server Behavior is not set. : Unable to receive events for the server. Please call the \"SetServerBehavior\" Method and set it up.");
            }

            StartListeners();
        }

        public ISocketServerWorker AddListener(ListenerConfig listenerConfig)
        {
            _listenerConfigs.Add(listenerConfig);
            return this;
        }

        public virtual ISocketServerWorker SetServerBehavior(IServerBehavior behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            this.behavior = behavior;

            return this;
        }

        public virtual ISocketServerWorker SetServerConfig(ISocketServerWorkerConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;

            return this;
        }

        public virtual ISocketServerWorker SetMsgFilterFactory(IMsgFilterFactory msgFilterFactory)
        {
            if (msgFilterFactory == null)
            {
                throw new ArgumentNullException(nameof(msgFilterFactory));
            }

            this.msgFilterFactory = msgFilterFactory;

            return this;
        }
#endregion ISocketServerWorker Method

        private void StartListeners()
        {
            var tempListeners = new List<IListener>();

            for (int index = 0; index < _listenerConfigs.Count; ++index)
            {
                var listenerConfig = listenerConfigs[index];
                if (listenerConfig == null)
                {
                    throw new Exception($"ListenerConfig is null : index({index})");
                }

                var listener = CreateListener();
                if (listener == null)
                {
                    throw new Exception($"Listener is null : index({index})");
                }

                listener.accepted = OnSocketAcceptedFromListeners;
                listener.error = OnErrorOccurredFromListeners;
                listener.Start(listenerConfig, service.loggerFactroy.GetLogger(logger.GetType()));

                tempListeners.Add(listener);

                logger.DebugFormat("Started listener : {0}", listenerConfig.ToString());
            }

            _listeners = tempListeners;
        }

        protected void OnSocketAcceptedFromListeners(IListener listener, Socket acceptedSocket)
        {
            TSession session = null;

            try
            {
                acceptedSocket.LingerState = new LingerOption(true, 0);

                acceptedSocket.SendBufferSize = config.sendBufferSize;
                acceptedSocket.ReceiveBufferSize = config.recvBufferSize;

                if (0 < config.sendTimeout)
                {
                    acceptedSocket.SendTimeout = config.sendTimeout;
                }

                if (0 < config.recvTimeout)
                {
                    acceptedSocket.ReceiveTimeout = config.recvTimeout;
                }

                acceptedSocket.NoDelay = config.noDelay;

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

                service.sessionConfigrator.Invoke(tempSession
                    .SetSocketServer(this)
                    .SetCloseHandler(OnCloseFromSocketSession));

                behavior?.OnSessionConnected(tempSession);

                // 시작하기전 상태를 체크합니다 None 상태가 아니라면 비정상적인 상황입니다.
                if (tempSession.state != ISocketSessionWorker.State.None)
                {
                    return;
                }

                tempSession.Start(acceptedSocket);
                
                // finally에서 오류 체크를 하기 위해 모든 작업이 성공적으로 끝난 후 대입해줍니다.
                session = tempSession;
            }
            catch (Exception ex)
            {
                behavior?.OnError(ex);
            }
            finally
            {
                // 세션을 생성하지 못하면 연결이 실패한 것으로 관리합니다.
                if (session == null)
                {
                    acceptedSocket?.SafeClose();
                }
            }
        }

        protected virtual void OnErrorOccurredFromListeners(IListener listener, Exception ex)
        {
            behavior?.OnError(ex);
        }

        protected virtual void OnCloseFromSocketSession(BaseSocketSessionWorker session)
        {
            behavior?.OnSessionDisconnected(session);
        }
        
        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 소켓 수락을 구현하는 <see cref="IListener"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract IListener CreateListener();

        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 연결된 소켓을 관리하는 <see cref="ISocketSessionWorker"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract TSession CreateSession();
    }
}