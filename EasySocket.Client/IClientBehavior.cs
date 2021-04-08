using System;

namespace EasySocket.Client
{
    public interface IClientBehavior
    {
        void OnStarted(IClient client);
        void OnStoped(IClient client);
        void OnReceived(IClient client, object msgFilter);
        void OnError(IClient client, Exception ex);
    }
}