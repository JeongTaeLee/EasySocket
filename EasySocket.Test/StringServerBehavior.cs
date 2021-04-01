using System;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class StringServerBehavior : IServerBehavior<string>
    {
        public Action<ISession<string>> onSsnConnected;
        public Action<ISession<string>> onSsnDisconnected;
        public Action<Exception> onError;

        public void OnSessionConnected(IServer<string> server, ISession<string> ssn)
        {
            onSsnConnected?.Invoke(ssn);
        }

        public void OnSessionDisconnected(IServer<string> server, ISession<string> ssn)
        {
            onSsnDisconnected?.Invoke(ssn);
        }

        public void OnError(IServer<string> server, Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}