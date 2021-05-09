using System;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public interface IClientBehaviour
    {
        Task OnStartedAsync(IClient client);
        Task OnStoppedAsync(IClient client);
        Task OnReceivedAsync(IClient client, object msgFilter);
        void OnError(IClient client, Exception ex);
    }
}