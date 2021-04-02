using System;

namespace EasySocket.Server
{
    public interface ISessionBehavior<TPacket>
    {
        void OnStartBefore(ISession<TPacket> ssn);
        void OnStartAfter(ISession<TPacket> ssn);
        
        void OnStopBefore(ISession<TPacket> ssn);
        void OnStopAfter(ISession<TPacket> ssn);

        void OnReceived(ISession<TPacket> ssn, TPacket packet);
        void OnError(ISession<TPacket> ssn, Exception ex);
    }
}