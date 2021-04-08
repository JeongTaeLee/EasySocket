using System;

namespace EasySocket.Server
{
    public interface ISessionBehavior
    {
        void OnStartBefore(ISession ssn);
        void OnStartAfter(ISession ssn);
        
        void OnStopBefore(ISession ssn);
        void OnStopAfter(ISession ssn);

        void OnReceived(ISession ssn, object packet);
        void OnError(ISession ssn, Exception ex);
    }
}