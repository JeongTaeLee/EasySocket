using System;
using EasySocket.Logging;
using EasySocket.Workers;

namespace EasySocket.Listeners
{
    public abstract class BaseListener : IListener
    {
#region IListener Field
        public ListenerConfig config { get; private set; }
        public ListenerAcceptHandler accepted {get; set;} = null;
        public ListenerErrorHandler error {get; set;} = null;
        #endregion

        protected ILogger logger { get; private set; } = null;

        public virtual void Start(ListenerConfig config, ILogger logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config)); 
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (string.IsNullOrEmpty(config.ip))
            {
                throw new ArgumentNullException(nameof(config.ip));
            }

            if (0 > config.port || short.MaxValue < config.port)
            {
                throw new ArgumentException("Invalid Port Range");
            }
  
            this.config = config;
            this.logger = logger;

            if (accepted == null)
            {
                this.logger.Warn("Accepted Handler is not set : Unable to receive events for socket accept");
            }

            if (error == null)
            {
                this.logger.Warn("Error Handler is not set : Unable to receive events for error");
            }
        }

        public virtual void Close()
        {
            config = null;
            accepted = null;
            error = null;
        }

    }
}