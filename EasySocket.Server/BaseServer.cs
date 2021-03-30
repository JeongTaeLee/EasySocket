using System;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters.Factories;

namespace EasySocket.Server
{
    public abstract class BaseServer<TServer, TSession, TPacket> : IServer<TPacket>
        where TServer : BaseServer<TServer, TSession, TPacket>
        where TSession : BaseSession<TSession, TPacket>
    {
        public abstract ServerState state { get; }

        protected ILoggerFactory loggerFactory { get; private set; } = null;
        protected ILogger logger { get; private set; } = null;

        public IMsgFilterFactory<TPacket> msgFilterFactory { get; private set; } = null;
        public IServerBehavior<TPacket> behavior { get; private set; } = null;
        public Action<ISession<TPacket>> sessionConfigrator { get; private set; } = null;

        public abstract ValueTask StartAsync();
        public abstract ValueTask StopAsync();

        protected virtual void InternalInitialize()
        {
            if (behavior == null)
            {
                logger.MemberNotSetWarn("Server Behavior", "SetServerBehavior");
            }

            if (loggerFactory == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("LoggerFactory", "SetLoggerFactory");
            }

            logger = loggerFactory.GetLogger(GetType());
            if (logger == null)
            {
                throw new InvalidOperationException("Unable to get logger from LoggerFactory");
            }

            if (msgFilterFactory == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("MsgFilterFactory", "SetMsgFilterFactory");
            }

            if (sessionConfigrator == null)
            {
                logger.MemberNotSetWarn("Session Configrator", "SetSessionConfigrator");
            }
        }
        public TServer SetMsgFilterFactory(IMsgFilterFactory<TPacket> msgFltrFctr)
        {
            msgFilterFactory = msgFltrFctr ?? throw new ArgumentNullException(nameof(msgFltrFctr));
            return this as TServer;
        }

        public TServer SetSessionConfigrator(Action<ISession<TPacket>> ssnCnfgr)
        {
            sessionConfigrator = ssnCnfgr ?? throw new ArgumentNullException(nameof(ssnCnfgr));
            return this as TServer;
        }
        public TServer SetServerBehavior(IServerBehavior<TPacket> bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as TServer;
        }

        public TServer SetLoggerFactory(ILoggerFactory lgrFctr)
        {
            loggerFactory = lgrFctr ?? throw new ArgumentNullException(nameof(lgrFctr));
            return this as TServer;
        }
    }
}