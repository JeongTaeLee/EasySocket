using System;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public interface IClientBehaviour
    {
        void OnStarted(IClient client);
        void OnStopped(IClient client);
        ValueTask OnReceived(IClient client, object msgFilter);
        void OnError(IClient client, Exception ex);
    }
}