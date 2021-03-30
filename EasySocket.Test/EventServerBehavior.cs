using System;
using EasySocket.Server;

namespace EasySocket.Test
{
    public class EventServerBehavior<TPacket> : IServerBehavior<TPacket>
    {
        private event Action<ISession<TPacket>> onSessionConnected;
        private event Action<ISession<TPacket>> onSessionDisconnected;
        private event Action<Exception> onError;

        public void OnSessionConnected(IServer<TPacket> server, ISession<TPacket> ssn)
        {
            onSessionConnected?.Invoke(ssn);
        }

        public void OnSessionDisconnected(IServer<TPacket> server, ISession<TPacket> ssn)
        {
            onSessionDisconnected?.Invoke(ssn);
        }

        public void OnError(IServer<TPacket> server, Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}