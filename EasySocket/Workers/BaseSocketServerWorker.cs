using System;
using System.Collections.Generic;
using System.Net.Sockets;
using EasySocket.Behaviors;
using EasySocket.Listeners;

namespace EasySocket.Workers
{
    public abstract class BaseSocketServerWorker : ISocketServerWorker
    {
        public IServerWorkerConfig config { get; private set; } = null;
        public EasySocketService service { get; private set; } = null;

        public IReadOnlyList<IListener> listeners { get; private set; } = null;

        public IServerBehavior behavior { get; private set; } = null;

        public void Start(EasySocketService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            this.service = service;

            StartListeners();
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

        public virtual ISocketServerWorker SetServerConfig(IServerWorkerConfig config)
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

            var listenerConfigs = service.config.listenerConfigs;

            for (int index = 0; index < listenerConfigs.Count; ++index)
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

            listeners = tempListeners;
        }

        /// <summary>
        /// <see cref="EasySocket.Listeners.IListener"/> 에서 새로운 소켓 수락 시 호출됩니다. 
        /// </summary>
        /// <param name="listener">수락된 <see cref="EasySocket.Listeners.IListener"/></param>
        /// <param name="acceptedSocket">수락된 <see cref="System.Net.Sockets.Socket"/></param>
        protected virtual void OnSocketAcceptedFromListeners(IListener listener, Socket acceptedSocket)
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
        }

        /// <summary>
        /// <see cref="EasySocket.Listeners.IListener"/> 에서 새로운 에러 발생 시 호출됩니다. 
        /// </summary>
        /// <param name="listener">발생된 <see cref="EasySocket.Listeners.IListener"/></param>
        /// <param name="ex">발생된 <see cref="System.Exception"/></param>
        protected virtual void OnErrorOccurredFromListeners(IListener listener, Exception ex)
        {
            behavior.OnError(ex);
        }

        /// <summary>
        /// 해당 클래스를 상속한 클래스가 해당 함수를 재정의하여 <see cref="EasySocket.Listeners.IListener"/>를 생성 후 반환합니다.
        /// </summary>
        /// <returns>생성된 <see cref="EasySocket.Listeners.IListener"/></returns>
        protected abstract IListener CreateListener();
                
        /// <summary>
        /// 해당 클래스를 상속한 클래스가 해당 함수를 재정의하여 <see cref="EasySocket.Workers.ISocketSessionWorker"/>를 생성 후 반환합니다.
        /// </summary>
        /// <returns>생성된 <see cref="EasySocket.Workers.ISocketSessionWorker"/></returns>
        protected abstract ISocketSessionWorker CreateSession();
    }
}