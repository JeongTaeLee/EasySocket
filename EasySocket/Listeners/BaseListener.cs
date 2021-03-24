using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;

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

        public virtual void Start(ListenerConfig cnfg, ILogger lger)
        {
            if (cnfg == null)
            {
                throw new ArgumentNullException(nameof(cnfg)); 
            }

            if (lger == null)
            {
                throw new ArgumentNullException(nameof(lger));
            }

            if (string.IsNullOrEmpty(cnfg.ip))
            {
                throw new ArgumentNullException(nameof(cnfg.ip));
            }

            if (0 > cnfg.port || short.MaxValue < cnfg.port)
            {
                throw new ArgumentException("Invalid Port Range");
            }
  
            config = cnfg;
            logger = lger;

            if (accepted == null)
            {
                logger.Warn("Accepted Handler is not set : Unable to receive events for socket accept");
            }

            if (error == null)
            {
                this.logger.Warn("Error Handler is not set : Unable to receive events for error");
            }
        }

        public abstract void Stop();
        public abstract Task StopAsync();
    }
}