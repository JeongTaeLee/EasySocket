using System;
using System.Collections.Generic;
using System.Net.Sockets;
using EasySocket.Behaviors;
using EasySocket.Listeners;

namespace EasySocket.Workers
{
    public abstract class BaseServerWorker : IServerWorker
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

        public virtual IServerWorker SetServerBehavior(IServerBehavior behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            this.behavior = behavior;

            return this;
        }

        public virtual IServerWorker SetServerWorkerConfig(IServerWorkerConfig config)
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

                listener.accepted = SocketAcceptedFromListener;
                listener.error = ErrorOccurredListener;

                listener.Start(listenerConfig);

                tempListeners.Add(listener);
            }

            listeners = tempListeners;
        }

        protected virtual void SocketAcceptedFromListener(IListener listener, Socket acceptedSocket)
        {
            
        }

        protected virtual void ErrorOccurredListener(IListener listener, Exception ex)
        {
            behavior.OnError(ex);
        }
 
        protected abstract IListener CreateListener();
    }
}