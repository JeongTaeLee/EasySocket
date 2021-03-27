using System;
using System.Buffers;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;
using EasySocket.Common.Extensions;

namespace EasySocket.Server
{
    public abstract class BaseSocketSession<TSession> : ISession<TSession>
        where TSession : BaseSocketSession<TSession>
    {
        
        public ISession.State state => (ISession.State)_state;
        public ISessionBehavior behavior { get; private set; } = null;

        private int _state = (int)IServer.State.None;
        private SemaphoreSlim _sendLock = null;
        private IMsgFilter _msgFilter  = null;
        private SessionStopHandler<TSession> _onStop = null;

        protected Socket socket { get; private set; } = null;
        protected ILogger logger { get; private set; } = null;

        public async ValueTask StartAsync(Socket sck)
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ISession.State.Starting, (int)ISession.State.None);
            if (prevState != (int)IServer.State.None)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(ISession.State)prevState}");
            }

            if (_msgFilter == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("MsgFilter", "SetMsgFilter");
            }

            if (_onStop == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("OnStop Callback", "SetOnStop");
            }

            if (logger == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("Logger", "SetLogger");
            }

            if (behavior == null)
            {
                logger.MemberNotSetWarn("Session Behavior", "SetSessionBehavior");
            }

            socket = sck ?? throw new ArgumentNullException(nameof(sck));
            
            _sendLock = new SemaphoreSlim(1, 1);
            
            await ProcessStart();

            _state = (int)ISession.State.Running;

            behavior?.OnStarted(this);
        }

        public async ValueTask StopAsync()
        {
            if (_state == (int)ISession.State.Running)
            {
                return;
            }

            await OnStop();
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry)
        {
            try
            {
                await _sendLock.WaitAsync();

                return await ProcessSend(mmry);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public TSession SetMsgFilter(IMsgFilter msgfltr)
        {
            _msgFilter = msgfltr ?? throw new ArgumentNullException(nameof(msgfltr));
            return this as TSession;
        }

        public TSession SetLogger(ILogger lgr)
        {
            logger = lgr ?? throw new ArgumentNullException(nameof(lgr));
            return this as TSession;
        }

        public TSession SetOnStop(SessionStopHandler<TSession> ssnStopHandler)
        {
            _onStop = ssnStopHandler ?? throw new ArgumentNullException(nameof(ssnStopHandler));
            return this as TSession;
        }

        public ISession SetSessionBehavior(ISessionBehavior bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as TSession;
        }

        protected virtual long OnReceive(ref ReadOnlySequence<byte> sequence)
        {
            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var msgInfo = _msgFilter.Filter(ref sequenceReader);

                    if (msgInfo == null)
                    {
                        break;
                    }

                    behavior?.OnReceived(this, msgInfo);
                }

                return (int)sequenceReader.Consumed;
            }
            catch (Exception ex)
            {
                behavior?.OnError(this, ex);

                return (int)sequence.Length;
            }
        }

        protected virtual void OnError(Exception ex)
        {
            behavior?.OnError(this, ex);
        }

        protected virtual async ValueTask OnStop()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ISession.State.Stopping, (int)ISession.State.Running);
            if (prevState != (int)ISession.State.Running)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(ISession.State)prevState}");
            }

            // 내부 종료
            await ProcessStop();

            // 종료 콜백 실행
            _onStop?.Invoke(this as TSession);

            // 변수 초기화.
            _sendLock = null;
            socket = null;
        
            // 상태 변경.
            _state = (int)ISession.State.Stopped;

            // Behavior 종료 콜백 실행.
            behavior?.OnStopped(this);
        }

        protected abstract ValueTask ProcessStart();
        protected abstract ValueTask ProcessStop();
        protected abstract ValueTask<int> ProcessSend(ReadOnlyMemory<byte> sendMemory);
    }
}