using System;
using System.Threading;
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
        public ServerState state => (ServerState)_state;

        private int _state = (int)ServerState.None;
        protected ISessionContainer<TSession> sessionContainer = new GUIDSessionContainer<TSession>();
        protected ILogger logger { get; private set; } = null;
        protected ILoggerFactory loggerFactory { get; private set; } = null;

        public IMsgFilterFactory<TPacket> msgFilterFactory { get; private set; } = null;
        public Action<ISession<TPacket>> sessionConfigrator { get; private set; } = null;
        public IServerBehavior<TPacket> behavior { get; private set; } = null;

        public async ValueTask StartAsync()
        {
            try
            {
                int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Starting, (int)ServerState.None);
                if (prevState != (int)ServerState.None)
                {
                    throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
                }

                InternalInitialize();

                await ProcessStart().ConfigureAwait(false);

                _state = (int)ServerState.Running;
            }
            finally
            {
                if (_state != (int)ServerState.Running)
                {
                    _state = (int)ServerState.None;
                }
            }
        }

        public  async ValueTask StopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ServerState.Stopping, (int)ServerState.Running);
            if (prevState != (int)ServerState.Running)
            {
                throw new InvalidOperationException($"The server has an invalid initial state. : Server state is {(ServerState)prevState}");
            }

            await ProcessStop();

            _state = (int)ServerState.Stopped;
        }

        protected virtual void InternalInitialize()
        {
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

            if (behavior == null)
            {
                logger.MemberNotSetWarn("Server Behavior", "SetServerBehavior");
            }
        }

        protected virtual void OnError(Exception ex)
        {
            behavior?.OnError(this, ex);
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

        protected abstract ValueTask ProcessStart();
        protected abstract ValueTask ProcessStop();
    }
}