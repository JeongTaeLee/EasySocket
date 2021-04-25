using System;
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

        public void OnReceived(IClient client, object msgFilter)
        {
            onReceived?.Invoke(client, msgFilter);
        }

        public void OnError(IClient client, Exception ex)
        {
            onError?.Invoke(client, ex);
        }
    }
}