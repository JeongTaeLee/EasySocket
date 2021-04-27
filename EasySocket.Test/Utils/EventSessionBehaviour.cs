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

        public event Action<ISession, object> onReceived;
        public event Action<Exception> onError;

        public ValueTask OnStartBeforeAsync(ISession ssn)
        {
            onStartBefore?.Invoke(ssn);
            return new ValueTask();
        }

        public ValueTask OnStartAfterAsync(ISession ssn)
        {
            onStartAfter?.Invoke(ssn);
            return new ValueTask();
        }

        public ValueTask OnStoppedAsync(ISession ssn)
        {
            onStopped?.Invoke(ssn);
            return new ValueTask();
        }

        public ValueTask OnReceivedAsync(ISession ssn, object packet)
        {
            onReceived?.Invoke(ssn, packet);
            return new ValueTask();
        }

        public void OnError(ISession ssn, Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}