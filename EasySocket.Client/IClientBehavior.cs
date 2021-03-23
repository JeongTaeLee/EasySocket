using System;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Client
{
    public interface IClientBehavior
    {
        void OnStarted(IClient client);
        void OnStoped(IClient client);
        void OnReceived(IClient client, IMsgFilter msgFilter);
        void OnError(IClient client, Exception ex);
    }
}