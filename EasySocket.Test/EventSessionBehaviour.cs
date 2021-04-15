using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class EventSessionBehaviour : ISessionBehaviour
    {
        public event Action<ISession> onStartBefore;
        public event Action<ISession> onStartAfter;

        public event Action<ISession> onStopped;

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

        public void OnStopped(ISession ssn)
        {
            onStopped?.Invoke(ssn);
        }

        public ValueTask OnReceived(ISession ssn, object packet)
        {
            onReceived?.Invoke(packet);
            return new ValueTask();
        }

        public void OnError(ISession ssn, Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}