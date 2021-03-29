using System;
using System.Threading.Tasks;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{
    public delegate void SessionStopHandler<TSession, TPacket>(TSession session) where TSession : BaseSession<TSession, TPacket>;

    public abstract class BaseSession<TSession, TPacket> : ISession<TPacket>
        where TSession : BaseSession<TSession, TPacket>
    {
        public abstract SessionState state { get; }
        public ISessionBehavior<TPacket> behavior { get; private set; } = null;

        public IMsgFilter<TPacket> msgFilter { get; private set; } = null;
        public SessionStopHandler<TSession, TPacket> _onStop { get; private set; } = null;

        public abstract ValueTask StopAsync();
        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);
        
        public ISession<TPacket> SetSessionBehavior(ISessionBehavior<TPacket> bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as ISession<TPacket>;
        }

        public TSession SetMsgFilter(IMsgFilter<TPacket> msgFltr)
        {
            msgFilter = msgFltr ?? throw new ArgumentNullException(nameof(msgFltr));
            return this as TSession;
        }
        
        public TSession SetOnStop(SessionStopHandler<TSession, TPacket> onStop)
        {
            _onStop = onStop ?? throw new ArgumentNullException(nameof(onStop));
            return this as TSession;
        }
    }
}