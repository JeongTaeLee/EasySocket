using System;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class EventSessionBehavior<TPacket> : ISessionBehavior<TPacket>
    {
        public event Action onStarted;
        public event Action onStopped;
        public event Action<TPacket> onReceived;
        public event Action<Exception> onError;

        public void OnStarted(ISession<TPacket> ssn)
        {
            onStarted?.Invoke();
        }

        public void OnStopped(ISession<TPacket> ssn)
        {
            onStopped?.Invoke();
        }

        public void OnReceived(ISession<TPacket> ssn, TPacket packet)
        {
            onReceived?.Invoke(packet);
        }
        public void OnError(ISession<TPacket> ssn, Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}