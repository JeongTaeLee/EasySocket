using System;

namespace EasySocket.Server
{
    public interface IServerBehavior<TPacket>
    {
        void OnSessionConnected(IServer<TPacket> server, ISession<TPacket> ssn);

        void OnSessionDisconnected(IServer<TPacket> server, ISession<TPacket> ssn);

        void OnError(IServer<TPacket> server, Exception ex);
    }
}