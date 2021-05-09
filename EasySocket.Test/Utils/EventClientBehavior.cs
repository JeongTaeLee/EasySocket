using System;
using System.Threading.Tasks;
using EasySocket.Client;

namespace EasySocket.Test
{
    public class EventClientBehavior : IClientBehaviour
    {
        public Action<IClient> onStarted { get; set; }
        public Action<IClient> onStopped { get; set; }
        public Action<IClient, object> onReceived { get; set; }
        public Action<IClient, Exception> onError { get; set; }

        public Task OnStartedAsync(IClient client)
        {
            onStarted?.Invoke(client);
            return Task.CompletedTask;
        }

        public Task OnStoppedAsync(IClient client)
        {
            onStopped?.Invoke(client);
            return Task.CompletedTask;

        }

        public Task OnReceivedAsync(IClient client, object msgFilter)
        {
            onReceived?.Invoke(client, msgFilter);
            return Task.CompletedTask;
        }

        public void OnError(IClient client, Exception ex)
        {
            onError?.Invoke(client, ex);
        }
    }
}