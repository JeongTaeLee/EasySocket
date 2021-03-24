using System;
using EasySocket.Common.Protocols.MsgInfos;

namespace EasySocket.Client
{
    public interface IClientBehavior
    {
        void OnStarted(IClient client);
        void OnStoped(IClient client);
        void OnReceived(IClient client, IMsgInfo msgFilter);
        void OnError(IClient client, Exception ex);
    }
}