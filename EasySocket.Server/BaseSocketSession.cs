using System;
using System.Buffers;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{
    public delegate void SocketSessionStopHandler<TSession>(TSession session) where TSession : BaseSocketSession<TSession>;

    public abstract class BaseSocketSession<TSession> : ISession<TSession>
        where TSession : BaseSocketSession<TSession>
    {
        
        public ISession.State state => (ISession.State)_state;
        public IMsgFilter msgFilter { get; private set; } = null;
        public ISessionBehavior behavior { get; private set; } = null;
        public ILogger logger { get; private set; } = null;

        private SemaphoreSlim _sendLock = null;
        private int _state = (int)IServer.State.None;

        protected Socket socket { get; private set; } = null;

        public SocketSessionStopHandler<TSession> onStop { get; set; } = null;

        public async ValueTask StartAsync(Socket sck)
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ISession.State.Starting, (int)ISession.State.None);
            if (prevState != (int)IServer.State.None)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(ISession.State)prevState}");
            }

            _sendLock = new SemaphoreSlim(1, 1);

            socket = sck;

            await ProcessStart();

            _state = (int)ISession.State.Running;
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

        protected virtual long OnReceive(ReadOnlySequence<byte> sequence)
        {
            try
            {
                return 0;
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
            onStop?.Invoke(this as TSession);

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