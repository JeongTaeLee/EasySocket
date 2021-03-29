using System;
using EasySocket.Common.Protocols.MsgInfos;

namespace EasySocket.Client
{
    public interface IClientBehavior<TPacket>
    {
        void OnStarted(IClient<TPacket> client);
        void OnStoped(IClient<TPacket> client);
        void OnReceived(IClient<TPacket> client, TPacket msgFilter);
        void OnError(IClient<TPacket> client, Exception ex);
    }
}