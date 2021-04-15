using System;
using System.Collections.Generic;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class EventSessionBehaviour : ISessionBehavior
    {
        public event Action<ISession> onStartBefore;
        public event Action<ISession> onStartAfter;

        public event Action<ISession> onStopBefore;
        public event Action<ISession> onStopAfter;

        public event Action<object> onReceived;
        public event Action<Exception> onError;

        public void OnStartBefore(ISession ssn)
        {
            onStartBefore?.Invoke(ssn);
        }

        public void OnStartAfter(ISession ssn)
        {
            onStartAfter?.Invoke(ssn);
        }

        public void OnStopBefore(ISession ssn)
        {
            onStopBefore?.Invoke(ssn);
        }

        public void OnStopAfter(ISession ssn)
        {
            onStopAfter?.Invoke(ssn);
        }

        public void OnReceived(ISession ssn, object packet)
        {
            onReceived?.Invoke(packet);
        }
        public void OnError(ISession ssn, Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}