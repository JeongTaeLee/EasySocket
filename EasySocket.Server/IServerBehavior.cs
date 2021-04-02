using System;

namespace EasySocket.Server
{
    public interface IServerBehavior<TPacket>
    {
        void OnError(IServer<TPacket> server, Exception ex);
    }
}