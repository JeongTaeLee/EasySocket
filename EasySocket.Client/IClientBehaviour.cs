using System;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public interface IClientBehaviour
    {
        ValueTask OnStartedAsync(IClient client);
        ValueTask OnStoppedAsync(IClient client);
        ValueTask OnReceivedAsync(IClient client, object msgFilter);
        void OnError(IClient client, Exception ex);
    }
}