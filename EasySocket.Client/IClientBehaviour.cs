using System;

namespace EasySocket.Client
{
    public interface IClientBehaviour
    {
        void OnStarted(IClient client);
        void OnStopped(IClient client);
        void OnReceived(IClient client, object msgFilter);
        void OnError(IClient client, Exception ex);
    }
}