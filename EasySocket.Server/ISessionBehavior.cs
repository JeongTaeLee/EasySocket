using System;

namespace EasySocket.Server
{
    public interface ISessionBehavior<TPacket>
    {
        void OnStarted(ISession<TPacket> ssn);
        void OnStopped(ISession<TPacket> ssn);
        void OnReceived(ISession<TPacket> ssn, TPacket packet);
        void OnError(ISession<TPacket> ssn, Exception ex);
    }
}