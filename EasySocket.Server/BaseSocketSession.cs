using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{
    public abstract class BaseSocketSession<TSession> : ISession<TSession>
        where TSession : BaseSocketSession<TSession>
    {
        public ISession.State state => (ISession.State)_state;
        public IMsgFilter msgFilter { get; private set; } = null;
        public ISessionBehavior behavior { get; private set; } = null;
        public ILogger logger { get; private set; } = null;

        private int _state = (int)IServer.State.None;

        protected Socket socket { get; private set; } = null;

        public async ValueTask StartAsync(Socket sck)
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ISession.State.Starting, (int)ISession.State.None);
            if (prevState != (int)IServer.State.None)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(ISession.State)prevState}");
            }

            socket = sck;

            await InternalStart();
        }

        public async ValueTask StopAsync()
        {
            await InternalStop();
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry)
        {
            throw new NotImplementedException();
        }

        public TSession SetMsgFilter(IMsgFilter msgfltr)
        {
            msgFilter = msgfltr ?? throw new ArgumentNullException(nameof(msgfltr));
            return this as TSession;
        }

        public TSession SetSessionBehavior(ISessionBehavior bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as TSession;
        }

        public TSession SetLogger(ILogger lgr)
        {
            logger = lgr ?? throw new ArgumentNullException(nameof(lgr));
            return this as TSession;
        }

        protected abstract ValueTask InternalStart();
        protected abstract ValueTask InternalStop();
    }
}