using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Server.Listeners
{
    public abstract class BaseListener : IListener
    {
        public ListenerConfig config { get; private set; } = null;
        public ListenerAcceptHandler onAccept { get; set; }
        public ListenerErrorHandler onError { get; set; }

        public async Task StartAsync(ListenerConfig cnfg)
        {
            config = cnfg ?? throw new ArgumentNullException(nameof(cnfg));
        
            await InternalStart();
        }

        public async Task StopAsync()
        {
            await InternalStop();   
        }

        protected void OnAccept(Socket sck)
        {
            onAccept?.Invoke(this, sck);
        }

        protected void OnError(Exception ex)
        {
            onError?.Invoke(this, ex);
        }

        protected abstract ValueTask InternalStart();
        protected abstract ValueTask InternalStop();
    }
}