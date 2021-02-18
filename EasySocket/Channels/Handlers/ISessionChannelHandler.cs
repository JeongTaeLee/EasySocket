using System;

namespace EasySocket.Channels.Handlers
{
    public interface ISessionChannelHandler
    {
        void OnSessionChannelStarted();
        void OnSessionChannelClosed();
        void OnSessionChannelError(Exception ex);
    }
}
