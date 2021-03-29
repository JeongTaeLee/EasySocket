using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;

namespace EasySocket.Server
{
    public abstract class SocketSession<TSession, TPacket> : BaseSession<TSession, TPacket>
        where TSession : BaseSession<TSession, TPacket>
    {
        public override SessionState state => (SessionState)_state;
    
        private int _state = (int)SessionState.None;
        private SemaphoreSlim _sendLock = null;

        protected Socket socket { get; private set; } = null;
        protected ILogger logger { get; private set; } = null;

        public async ValueTask StartAsync(Socket sck)
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Starting, (int)SessionState.None);
            if (prevState != (int)SessionState.None)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(SessionState)prevState}");
            }

            if (msgFilter == null)
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

            _state = (int)SessionState.Running;

            behavior?.OnStarted(this);
        }

        public override async ValueTask StopAsync()
        {
            if (_state == (int)SessionState.Running)
            {
                return;
            }

            await OnStop();
        }

        public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry)
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

        protected virtual async ValueTask OnStop()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Stopping, (int)SessionState.Running);
            if (prevState != (int)SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(SessionState)prevState}");
            }

            // 내부 종료
            await ProcessStop();

            // 변수 초기화.
            _sendLock = null;
            socket = null;

            // 상태 변경.
            _state = (int)SessionState.Stopped;

            // Behavior 종료 콜백 실행.
            behavior?.OnStopped(this);

            // 완전 종료 콜백 실행
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
                behavior?.OnError(this, ex);

                return sequence.Length;
            }
        }

        protected virtual void OnError(Exception ex)
        {
            behavior?.OnError(this, ex);
        }

        public TSession SetLogger(ILogger lgr)
        {
            logger = lgr ?? throw new ArgumentNullException(nameof(lgr));
            return this as TSession;
        }

        protected abstract ValueTask ProcessStart();
        protected abstract ValueTask ProcessStop();
        protected abstract ValueTask<int> ProcessSend(ReadOnlyMemory<byte> sendMemory);
    }
}