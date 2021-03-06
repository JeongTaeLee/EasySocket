using System;
using System.Collections.Generic;
using System.Net.Sockets;
using EasySocket.Behaviors;
using EasySocket.Listeners;
using EasySocket.Protocols.Filters.Factories;

namespace EasySocket.Workers
{
    public abstract class BaseSocketServerWorker<TSession> : ISocketServerWorker
        where TSession : BaseSocketSessionWorker
    {
#region ISocketServerWorker Field 
        public ISocketServerWorkerConfig config { get; private set; } = new SocketServerWorkerConfig();
        public EasySocketService service { get; private set; } = null;
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;
        public IServerBehavior behavior { get; private set; } = null;
        public IMsgFilterFactory msgFilterFactory { get; private set; }
        #endregion ISocketServerWorker Field

        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private IReadOnlyList<IListener> _listeners = null;

#region ISocketServerWorker Method
        public void Start(EasySocketService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            this.service = service;

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

                listener.Start(listenerConfig);

                tempListeners.Add(listener);
            }

            _listeners = tempListeners;
        }

        protected void OnSocketAcceptedFromListeners(IListener listener, Socket acceptedSocket)
        {
            TSession session = null;

            try
            {
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

                var tempSession = CreateSession();
                if (tempSession == null)
                {
                    return;
                }

                tempSession.Initialize(this, acceptedSocket);

                service.sessionConfigrator.Invoke(tempSession);

                tempSession.Start();

                // finally에서 오류 체크를 하기 위해 모든 작업이 성공적으로 끝난 후 대입해줍니다.
                session = tempSession;
            }
            catch (Exception ex)
            {
                behavior.OnError(ex);
            }
            finally
            {
                // 세션을 생성하지 못하면 연결이 실패한 것으로 관리합니다.
                if (session == null)
                {
                    try
                    {
                        acceptedSocket?.Shutdown(SocketShutdown.Both);
                    }
                    finally
                    {
                        acceptedSocket?.Close();
                    }
                }
            }
        }

        protected virtual void OnErrorOccurredFromListeners(IListener listener, Exception ex)
        {
            behavior.OnError(ex);
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