using System;
using System.Threading.Tasks;

namespace EasySocket.Server
{
    public interface ISessionBehaviour
    {
        void OnStartBefore(ISession ssn);
        void OnStartAfter(ISession ssn);

        void OnStopped(ISession ssn);

        ValueTask OnReceived(ISession ssn, object packet);
        void OnError(ISession ssn, Exception ex);
    }
}