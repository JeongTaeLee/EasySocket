using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{

    public delegate void SessionStopHandler<TSession>(TSession session) where TSession : ISession<TSession>;

    public interface ISession<TSession> : ISession
        where TSession : ISession<TSession>
    {
        TSession SetMsgFilter(IMsgFilter msgfltr);
        TSession SetLogger(ILogger logger);
        TSession SetOnStop(SessionStopHandler<TSession> onClose);
    }

    public interface ISession
    {
        public enum State
        {
            None = 0,
            Starting,
            Running,
            Stopping,
            Stopped,
        }

        State state { get; }
        ISessionBehavior behavior { get; }

        ValueTask StopAsync();
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        ISession SetSessionBehavior(ISessionBehavior bhvr);
    }
}