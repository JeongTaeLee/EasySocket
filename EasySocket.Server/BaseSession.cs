using System;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{
    public delegate void SessionStopHandler<TSession, TPacket>(TSession session) where TSession : BaseSession<TSession, TPacket>;

    public abstract class BaseSession<TSession, TPacket> : ISession<TPacket>
        where TSession : BaseSession<TSession, TPacket>
    {
        public string sessionId { get; private set; } = string.Empty;
        public abstract SessionState state { get; }
        public ISessionBehavior<TPacket> behavior { get; private set; } = null;

        protected ILogger logger { get; private set; } = null;

        public IMsgFilter<TPacket> msgFilter { get; private set; } = null;
        public SessionStopHandler<TSession, TPacket> _onStop { get; private set; } = null;


        public abstract ValueTask StopAsync();
        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);
        public ISession<TPacket> SetSessionBehavior(ISessionBehavior<TPacket> bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as ISession<TPacket>;
        }

        protected virtual void InternalInitialize()
        {
            if (sessionId == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("SessionId", "SetSessionId");
            }

            if (logger == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("Logger", "SetLogger");
            }

            if (msgFilter == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("MsgFilter", "SetMsgFilter");
            }

            if (_onStop == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("OnStop Callback", "SetOnStop");
            }

            if (behavior == null)
            {
                logger.MemberNotSetWarn("Session Behavior", "SetSessionBehavior");
            }
        }

        public TSession SetSessionId(string ssnId)
        {
            if (string.IsNullOrEmpty(ssnId))
            {
                throw new ArgumentNullException(nameof(ssnId));
            }

            sessionId = ssnId;

            return this as TSession;
        }

        public TSession SetLogger(ILogger lgr)
        {
            logger = lgr ?? throw new ArgumentNullException(nameof(lgr));
            return this as TSession;
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