using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{
    public interface ISession<TSession> : ISession
        where TSession : ISession<TSession>
    {
        TSession SetMsgFilter(IMsgFilter msgfltr);
        TSession SetLogger(ILogger logger);
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
        IMsgFilter msgFilter { get; }
        ISessionBehavior behavior { get; }
        ILogger logger { get; }

        ValueTask StopAsync();
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        ISession SetSessionBehavior(ISessionBehavior bhvr);
    }
}