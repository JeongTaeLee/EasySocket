using System;
using EasySocket.Workers;

namespace EasySocket.Listeners
{
    public abstract class BaseListener : IListener
    {
        public ListenerConfig config { get; private set; }
        public IServerWorker server { get; private set; }

        public ListenerAcceptHandler accepted {get; set;} = null;
        public ListenerErrorHandler error {get; set;} = null;

        protected BaseListener(IServerWorker server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            this.server = server;
        }

        public virtual void Start(ListenerConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config)); 
            }

            if (string.IsNullOrEmpty(config.ip))
            {
                throw new ArgumentNullException(nameof(config.ip));
            }

            if (0 > config.port || short.MaxValue < config.port){
                throw new ArgumentException("Invalid Port Range");
            }
  
            this.config = config;            
        }

        public virtual void Close()
        {
            config = null;
            accepted = null;
            error = null;
        }
    }
}