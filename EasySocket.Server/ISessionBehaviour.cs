using System;
using System.Threading.Tasks;

namespace EasySocket.Server
{
    public interface ISessionBehaviour
    {
        ValueTask OnStartBeforeAsync(ISession session);
        ValueTask OnStartAfterAsync(ISession session);
        ValueTask OnStoppedAsync(ISession session);
        ValueTask OnReceivedAsync(ISession session, object packet);
        void OnError(ISession session, Exception ex);
    }
}
