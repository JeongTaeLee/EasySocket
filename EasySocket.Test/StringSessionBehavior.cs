using System;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class StringSessionBehavior : ISessionBehavior<string>
    {
        public Action onStarted;
        public Action onStopped;
        public Action<string> onReceived;
        public Action<Exception> onError;

        public void OnStarted(ISession<string> ssn)
        {
            onStarted?.Invoke();
        }

        public void OnStopped(ISession<string> ssn)
        {
            onStopped?.Invoke();
        }

        public void OnReceived(ISession<string> ssn, string packet)
        {
            onReceived?.Invoke(packet);
        }

        public void OnError(ISession<string> ssn, Exception ex)
        {
            onError?.Invoke(ex);
        }        
    }
}