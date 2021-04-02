using System;
using System.Buffers;
using System.Threading;
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
        public SessionState state => (SessionState)_state;
        public ISessionBehavior<TPacket> behavior { get; private set; } = null;

        private int _state = (int)SessionState.None;

        protected ILogger logger { get; private set; } = null;

        public IMsgFilter<TPacket> msgFilter { get; private set; } = null;
        public SessionStopHandler<TSession, TPacket> _onStop { get; private set; } = null;

        public async ValueTask StartAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Starting, (int)SessionState.None);
            if (prevState != (int)SessionState.None)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(SessionState)prevState}");
            }

            InternalInitialize();

            await ProcessStart();

            _state = (int)SessionState.Running;

            behavior?.OnStarted(this);
        }

        public async ValueTask StopAsync()
        {
            if (_state != (int)SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid state. : Session state is {(SessionState)_state}");
            }

            await OnStop();
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

        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        protected async ValueTask OnStop()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Stopping, (int)SessionState.Running);
            if (prevState != (int)SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid state. : Session state is {(SessionState)prevState}");
            }

            await ProcessStop();

            _state = (int)SessionState.Stopped;

            behavior?.OnStopped(this);

            _onStop?.Invoke(this as TSession);
        }

        protected virtual long OnReceive(ref ReadOnlySequence<byte> sequence)
        {
            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var packet = msgFilter.Filter(ref sequenceReader);
                    if (packet == null)
                    {
                        return sequence.Length;
                    }

                    behavior?.OnReceived(this, packet);
                }

                return (int)sequenceReader.Consumed;
            }
            catch (Exception ex)
            {
                OnError(ex);
                return sequence.Length;
            }
        }

        protected virtual void OnError(Exception ex)
        {
            behavior?.OnError(this, ex);
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

        public ISession<TPacket> SetSessionBehavior(ISessionBehavior<TPacket> bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as ISession<TPacket>;
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

        protected abstract ValueTask ProcessStart();
        protected abstract ValueTask ProcessStop();
    }
}