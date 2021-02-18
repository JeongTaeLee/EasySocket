using System;

namespace EasySocket.Channels.Handlers
{
    public interface IServerChannelHandler
    {
        void OnNewSessionChannelConnected(ISessionChannel channel);

        void OnSessionChannelDisconnected(ISessionChannel channel);

        void OnServerChannelError(Exception ex);
    }
}
