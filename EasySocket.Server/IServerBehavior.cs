using System;

namespace EasySocket.Server
{
    public interface IServerBehavior
    {
        void OnSessionConnected(IServer session);

        void OnSessionDisconnected(IServer session);

        void OnError(IServer session, Exception ex);
    }
}