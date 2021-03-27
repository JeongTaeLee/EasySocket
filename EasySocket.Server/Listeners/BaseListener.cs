using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Common.Logging;

namespace EasySocket.Server.Listeners
{
    public abstract class BaseListener : IListener
    {
        public ListenerConfig config { get; private set; } = null;
        public ListenerAcceptHandler onAccept { get; set; }
        public ListenerErrorHandler onError { get; set; }

        protected ILogger logger { get; set; } = null;

        public async Task StartAsync(ListenerConfig cnfg, ILogger lger)
        {
            config = cnfg ?? throw new ArgumentNullException(nameof(cnfg));
            logger = lger ?? throw new ArgumentNullException(nameof(lger));
            
            if (string.IsNullOrEmpty(cnfg.ip))
            {
                throw new ArgumentNullException(nameof(cnfg.ip));
            }

            if (0 > cnfg.port || short.MaxValue < cnfg.port)
            {
                throw new ArgumentException("Invalid Port Range");
            }

            if (onAccept == null)
            {
                logger.Warn("Accepted Handler is not set : Unable to receive events for socket accept");
            }

            if (onError == null)
            {
                logger.Warn("Error Handler is not set : Unable to receive events for error");
            }

            await InternalStart();
        }

        public async Task StopAsync()
        {
            await InternalStop();   
        }

        protected async void OnAccept(Socket sck)
        {
            if (onAccept == null)
            {
                return;
            }

            await onAccept.Invoke(this, sck);
        }

        protected void OnError(Exception ex)
        {
            onError?.Invoke(this, ex);
        }

        protected abstract ValueTask InternalStart();
        protected abstract ValueTask InternalStop();
    }
}