using System;
using System.Collections.Generic;
using System.Linq;
using EasySocket.Listeners;
using EasySocket.Workers;

namespace EasySocket
{    public class EasySocketConfig
    {
        public readonly IReadOnlyList<ListenerConfig> listenerConfigs;
        public readonly Func<IServerWorker> serverGenerator;

        private EasySocketConfig(IReadOnlyList<ListenerConfig> listenerConfigs, Func<IServerWorker> serverGenerator)
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

            public Func<IServerWorker> serverGenerator { get; private set; } = null;
  

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
            
            public EasySocketConfig.Builder SetServerGenerator(Func<IServerWorker> serverGenerator)
            {
                if (serverGenerator == null)
                {
                    throw new ArgumentNullException(nameof(serverGenerator));
                }

                this.serverGenerator = serverGenerator;

                return this; 
            }  

            public EasySocketConfig.Builder SetServerGenerator<TServer>()
                where TServer : class, IServerWorker, new()
            {
                this.serverGenerator = () =>
                {
                    return new TServer();
                };

                return this;
            }
        }
    }
}