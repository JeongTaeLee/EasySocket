using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters.Factories;

namespace EasySocket.Server
{
    public abstract class BaseServer<TServer, TSession, TPacket> : IServer<TPacket>
        where TServer : BaseServer<TServer, TSession, TPacket>
        where TSession : BaseSession<TSession, TPacket>
    {
        public abstract ServerState state { get; }
        public IServerBehavior<TPacket> behavior { get; private set; } = null;

        public IMsgFilterFactory<TPacket> msgFilterFactory { get; private set; } = null;
        public ILoggerFactory loggerFactory { get; private set; } = null;
        public Action<ISession<TPacket>> sessionConfigrator { get; private set; } = null;

        public abstract ValueTask StartAsync();
        public abstract ValueTask StopAsync();

        public IServer<TPacket> SetServerBehavior(IServerBehavior<TPacket> bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this;
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

        public TServer SetLoggerFactory(ILoggerFactory lgrFctr)
        {
            loggerFactory = lgrFctr ?? throw new ArgumentNullException(nameof(lgrFctr));
            return this as TServer;
        }
    }
}