using System;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class EventServerBehavior<TPacket> : ISessionBehavior<TPacket>
    {
        private event Action<ISession<TPacket>> onStated;
        private event Action<ISession<TPacket>> onStopped;
        private event Action<ISession<TPacket>, TPacket> onReceived;
        private event Action<ISession<TPacket>, Exception> onError;

        public void OnStarted(ISession<TPacket> ssn)
        {
            onStated?.Invoke(ssn);
        }

        public void OnStopped(ISession<TPacket> ssn)
        {
            onStopped?.Invoke(ssn);
        }

        public void OnReceived(ISession<TPacket> ssn, TPacket packet)
        {
            onReceived?.Invoke(ssn, packet);
        }

        public void OnError(ISession<TPacket> ssn, Exception ex)
        {
            onError?.Invoke(ssn, ex);
        }
    }
}