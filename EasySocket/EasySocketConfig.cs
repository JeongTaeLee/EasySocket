using System;
using System.Collections.Generic;
using System.Linq;
using EasySocket.Behaviors;
using EasySocket.Listeners;
using EasySocket.Workers;

namespace EasySocket
{    public class EasySocketConfig
    {
        public readonly IReadOnlyList<ListenerConfig> listenerConfigs;
        public readonly Func<ISocketServerWorker> serverGenerator;

        private EasySocketConfig(IReadOnlyList<ListenerConfig> listenerConfigs, Func<ISocketServerWorker> serverGenerator)
        {
            if (listenerConfigs == null)
            {
                throw new ArgumentNullException(nameof(listenerConfigs));
            }

            if (serverGenerator == null)
            {
                throw new ArgumentNullException(nameof(serverGenerator));
            }

            if (0 >= listenerConfigs.Count)
            {
                throw new ArgumentException("At least one listener must be set.");
            }

            this.listenerConfigs = listenerConfigs;
            this.serverGenerator = serverGenerator;
        }

        public class Builder
        {
            private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
            
            public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;

            public Func<ISocketServerWorker> serverGenerator { get; private set; } = null;
  

            public Builder()
            {

            }
            
            public EasySocketConfig Build()
            {
                return new EasySocketConfig(
                    _listenerConfigs.ToList(),
                    serverGenerator
                );
            }

            public EasySocketConfig.Builder AddListener(ListenerConfig listenerConfig)
            {
                if (listenerConfig == null)
                {
                    throw new ArgumentNullException(nameof(listenerConfig));
                }

                _listenerConfigs.Add(listenerConfig);

                return this;
            }

            public int RemoveListenerConfig(Func<ListenerConfig, bool> predicate)
            {
                if (predicate == null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                return _listenerConfigs.RemoveAll(x => predicate(x));
            }
            
            public EasySocketConfig.Builder SetServerGenerator(Func<ISocketServerWorker> serverGenerator)
            {
                if (serverGenerator == null)
                {
                    throw new ArgumentNullException(nameof(serverGenerator));
                }

                this.serverGenerator = serverGenerator;

                return this; 
            }  

            public EasySocketConfig.Builder SetServerGenerator<TServer>(ISocketServerWorkerConfig config, IServerBehavior behavior)
                where TServer : class, ISocketServerWorker, new()
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                if (behavior == null)
                {
                    throw new ArgumentNullException(nameof(behavior));
                }

                this.serverGenerator = () =>
                {
                    return new TServer()
                        .SetServerConfig(config)
                        .SetServerBehavior(behavior);
                };

                return this;
            }
        }
    }
}