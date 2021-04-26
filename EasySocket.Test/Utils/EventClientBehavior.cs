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

        public void OnStarted(IClient client)
        {
            onStarted?.Invoke(client);
        }

        public void OnStopped(IClient client)
        {
            onStopped?.Invoke(client);
        }

        public ValueTask OnReceived(IClient client, object msgFilter)
        {
            onReceived?.Invoke(client, msgFilter);
            return new ValueTask();
        }

        public void OnError(IClient client, Exception ex)
        {
            onError?.Invoke(client, ex);
        }
    }
}