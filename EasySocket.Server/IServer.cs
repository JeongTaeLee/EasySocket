using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters.Factories;

namespace EasySocket.Server
{
    public enum ServerState
    {
        None = 0,
        Starting,
        Running,
        Stopping,
        Stopped,
    }

    public interface IServer<TPacket> : _IServer
    {
        IServerBehavior<TPacket> behavior { get; }

        IServer<TPacket> SetServerBehavior(IServerBehavior<TPacket> bhvr);
    }

    public interface _IServer
    {
        ServerState state { get; }

        ValueTask StartAsync();
        ValueTask StopAsync();
    }
}