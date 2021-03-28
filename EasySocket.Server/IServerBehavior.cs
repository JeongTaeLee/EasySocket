using System;

namespace EasySocket.Server
{
    public interface IServerBehavior
    {
        void OnSessionConnected(IServer server, ISession ssn);

        void OnSessionDisconnected(IServer server, ISession ssn);

        void OnError(IServer server, Exception ex);
    }
}