using System;
using System.Collections.Generic;
using System.Net.Sockets;
using EasySocket.Behaviors;
using EasySocket.Listeners;

namespace EasySocket.Workers
{
    public abstract class BaseSocketServerWorker<TSession> : ISocketServerWorker
        where TSession : BaseSocketSessionWorker
    {
        public ISocketServerWorkerConfig config { get; private set; } = null;
        public EasySocketService service { get; private set; } = null;
        public IServerBehavior behavior { get; private set; } = null;
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;

        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private IReadOnlyList<IListener> _listeners = null;

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

        /// <summary>
        /// <see cref="IListener"/> 에서 새로운 소켓 수락 시 호출됩니다. 
        /// </summary>
        /// <param name="listener">수락된 <see cref="IListener"/></param>
        /// <param name="acceptedSocket">수락된 <see cref="System.Net.Sockets.Socket"/></param>
        protected virtual void OnSocketAcceptedFromListeners(IListener listener, Socket acceptedSocket)
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

                var tempSession = CreateSession(acceptedSocket);
                if (tempSession == null)
                {
                    return;
                }

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

        /// <summary>
        /// <see cref="IListener"/> 에서 새로운 에러 발생 시 호출됩니다. 
        /// </summary>
        /// <param name="listener">발생된 <see cref="IListener"/></param>
        /// <param name="ex">발생된 <see cref="System.Exception"/></param>
        protected virtual void OnErrorOccurredFromListeners(IListener listener, Exception ex)
        {
            behavior.OnError(ex);
        }

        /// <summary>
        /// 해당 클래스를 상속한 클래스가 해당 함수를 재정의하여 <see cref="IListener"/>를 생성 후 반환합니다.
        /// </summary>
        /// <returns>생성된 <see cref="IListener"/></returns>
        protected abstract IListener CreateListener();

        /// <summary>
        /// 해당 클래스를 상속한 클래스가 해당 함수를 재정의하여 <see cref="ISocketSessionWorker"/>를 생성 후 반환합니다.
        /// </summary>
        /// <returns>생성된 <see cref="ISocketSessionWorker"/></returns>
        protected abstract TSession CreateSession(Socket socket);

    }
}