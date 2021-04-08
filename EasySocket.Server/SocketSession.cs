using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{

    public delegate void SocketSessionStopHandler<TSession>(TSession session);

    public abstract class SocketSession<TSession> : ISession
        where TSession : SocketSession<TSession>
    {
        public string sessionId => parameter.sessionId;
        public SessionState state => (SessionState)_state;
        public ISessionBehavior behavior { get; private set; } = null;

        private int _state = (int)SessionState.None;
        
        protected Socket socket { get; private set; } = null;
        protected SessionParameter<TSession> parameter { get; private set; } = default;

        public async ValueTask StartAsync(Socket sck, SessionParameter<TSession> ssnPrmtr)
        {
            socket = sck ?? throw new ArgumentNullException(nameof(sck));
            parameter = ssnPrmtr ?? throw new ArgumentNullException(nameof(ssnPrmtr));

            if (behavior == null)
            {
                parameter.logger.MemberNotSetWarn("Session Behavior", "SetSessionBehavior");
            }

            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Starting, (int)SessionState.None);
            if (prevState != (int)SessionState.None)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(SessionState)prevState}");
            }

            behavior?.OnStartBefore(this);

            await ProcessStart();

            _state = (int)SessionState.Running;

            behavior?.OnStartAfter(this);
        }

        public async ValueTask StopAsync()
        {
            if (_state != (int)SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid state. : Session state is {(SessionState)_state}");
            }

            await OnStop();
        }

        protected async ValueTask OnStop()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Stopping, (int)SessionState.Running);
            if (prevState != (int)SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid state. : Session state is {(SessionState)prevState}");
            }

            behavior?.OnStopBefore(this);

            await ProcessStop();

            _state = (int)SessionState.Stopped;

            behavior?.OnStopAfter(this);

            parameter.onStop.Invoke(this as TSession);
        }

        protected virtual long OnReceive(ref ReadOnlySequence<byte> sequence)
        {
            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var packet = parameter.msgFilter.Filter(ref sequenceReader);
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

        protected virtual ValueTask ProcessStart()
        {
            return new ValueTask();
        }

        protected virtual ValueTask ProcessStop()
        {
            return new ValueTask();
        }

        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);
        
        public ISession SetSessionBehavior(ISessionBehavior bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this;
        }
    }
}