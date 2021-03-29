using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{
    public enum SessionState
    {
        None = 0,
        Starting,
        Running,
        Stopping,
        Stopped,
    }

    public delegate void SessionStopHandler<TSession, TPacket>(TSession session) where TSession : ISession<TSession, TPacket>;

    public interface ISession<TSession, TPacket> : ISession<TPacket>
        where TSession : ISession<TSession, TPacket>
    {
        TSession SetMsgFilter(IMsgFilter<TPacket> msgfltr);
        TSession SetOnStop(SessionStopHandler<TSession, TPacket> onClose);
        TSession SetLogger(ILogger logger);
    }

    public interface ISession<TPacket>
    {
        SessionState state { get; }
        ISessionBehavior<TPacket> behavior { get; }

        ValueTask StopAsync();
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        ISession<TPacket> SetSessionBehavior(ISessionBehavior<TPacket> bhvr);
    }
}