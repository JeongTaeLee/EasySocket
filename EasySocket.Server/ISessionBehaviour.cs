using System;
using System.Threading.Tasks;

namespace EasySocket.Server
{
    public interface ISessionBehaviour
    {
        ValueTask OnStartBeforeAsync(ISession ssn);
        ValueTask OnStartAfterAsync(ISession ssn);
        ValueTask OnStoppedAsync(ISession ValueTask);
        ValueTask OnReceivedAsync(ISession ssn, object packet);
        void OnError(ISession ssn, Exception ex);
    }
}