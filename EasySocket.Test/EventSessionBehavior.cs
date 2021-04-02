using System;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class EventSessionBehavior<TPacket> : ISessionBehavior<TPacket>
    {
        public event Action<ISession<TPacket>> onStartBefore;
        public event Action<ISession<TPacket>> onStartAfter;

        public event Action<ISession<TPacket>> onStopBefore;
        public event Action<ISession<TPacket>> onStopAfter;

        public event Action<TPacket> onReceived;
        public event Action<Exception> onError;


        public void OnStartBefore(ISession<TPacket> ssn)
        {
            onStartBefore?.Invoke(ssn);
        }

        public void OnStartAfter(ISession<TPacket> ssn)
        {
            onStartAfter?.Invoke(ssn);
        }

        public void OnStopBefore(ISession<TPacket> ssn)
        {
            onStopBefore?.Invoke(ssn);
        }

        public void OnStopAfter(ISession<TPacket> ssn)
        {
            onStopAfter?.Invoke(ssn);
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