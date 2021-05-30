using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;

namespace EasySocket.Server
{

    public delegate void SocketSessionStopHandler<TSession>(TSession session);

    public abstract class SocketSession<TSession> : ISession
        where TSession : SocketSession<TSession>
    {
        public string id => param.sessionId;
        public SessionState state => (SessionState)_state;
        public ISessionBehaviour behaviour { get; private set; } = null;

        private int _state = (int)SessionState.None;
        protected SessionParameter<TSession> param { get; private set; } = null;
        protected Socket socket { get; private set; } = null;

        public virtual async ValueTask StartAsync(SessionParameter<TSession> prm, Socket sck)
        {
            if (state == SessionState.Stopped)
            {
                throw ExceptionExtensions.TerminatedObjectIOE("Session");
            }
            
            param = prm ?? throw new ArgumentNullException(nameof(prm));
            socket = sck ?? throw new ArgumentNullException(nameof(sck));

            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Starting, (int)SessionState.None);
            if (prevState != (int)SessionState.None)
            {
                throw ExceptionExtensions.CantStartObjectIOE("Session", (SessionState)prevState);
            }

            if (behaviour == null)
            {
                param.logger.MemberNotSetUseMethodWarn("Session Behaviour", "SetSessionBehaviour");
            }

            try
            {
                if (behaviour != null)
                {
                    await behaviour.OnStartBeforeAsync(this);
                }

                await InternalStartAsync();
                _state = (int)SessionState.Running;

                await OnStartedAsync();
            }
            finally
            {
                if (state != SessionState.Running)
                {
                    _state = (int)SessionState.None;
                }
            }
        }
        
        public virtual async ValueTask StopAsync()
        {
            if (_state != (int)SessionState.Running)
            {
                throw ExceptionExtensions.CantStopObjectIOE("Session", state);
            }

            await ProcessStopAsync();
        }

        /// <summary>
        /// 세션이 시작된 후 외부(<see cref="IServer"/>) 계열 클래스 에서 호출되는 메서드
        /// </summary>
        protected virtual async ValueTask OnStartedAsync()
        {
            if (state != SessionState.Running)
            {
                ExceptionExtensions.InvalidObjectStateIOE("Session", state);
                return;
            }

            if (behaviour != null)
            {
                await behaviour.OnStartAfterAsync(this);
            }
        }

        /// <summary>
        /// 세션이 종료된 후 외부(<see cref="IServer"/>) 계열 클래스 에서 호출되는 메서드
        /// </summary>
        protected virtual async ValueTask OnStoppedAsync()
        {
            if (state != SessionState.Stopped)
            {
                ExceptionExtensions.InvalidObjectStateIOE("Session", state);
                return;
            }

            if (behaviour != null)
            {
                await behaviour.OnStoppedAsync(this);
            }
        }

        /// <summary>
        /// 내부에서 사용하는 종료 메서드.
        /// </summary>
        protected async ValueTask ProcessStopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Stopping, (int)SessionState.Running);
            if (prevState != (int)SessionState.Running)
            {
                param.logger.InvalidObjectStateError("Server", (SessionState)prevState); 
                return;
            }
            
            socket?.SafeClose();

            await InternalStopAsync();

            _state = (int)SessionState.Stopped;

            await OnStoppedAsync();

            param?.onStop?.Invoke(this as TSession);
        }

        /// <summary>
        /// 데이터 수신시 호출되는 메서드.
        /// </summary>
        protected async Task<ReadOnlySequence<byte>> ProcessReceive(ReadOnlySequence<byte> sequence)
        {
            if (state != SessionState.Running)
            {
                ExceptionExtensions.InvalidObjectStateIOE("Session", state);
            }

            while (sequence.Length > 0)
            {
                var packet = param.msgFilter.Filter(ref sequence);
                if (packet == null)
                {
                    break;
                }

                if (behaviour != null)
                {
                    await behaviour.OnReceivedAsync(this, packet);
                }
            }

            return sequence;
        }

        /// <summary>
        /// 내부 오류 발생시 호출되는 메서드.
        /// </summary>
        protected void ProcessError(Exception ex)
        {
            behaviour?.OnError(this as TSession, ex);
        }


        /// <summary>
        /// 하위 객체에서 정의하는 데이터 동기 전송 메서드.
        /// </summary>
        public abstract int Send(byte[] buffer);

        /// <summary>
        /// 하위 객체에서 정의하는 데이터 동기 전송 메서드.
        /// </summary>
        public abstract int Send(byte[] buffer, int offset, int count);

        /// <summary>
        /// 하위 객체에서 정의하는 데이터 동기 전송 메서드.
        /// </summary>
        public abstract int Send(ArraySegment<byte> segment);

        /// <summary>
        /// 하위 객체에서 정의하는 데이터 전송 메서드.
        /// </summary>
        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        /// <summary>
        /// 하위 객체에서 정의하는 시작시 호출 메서드.
        /// </summary>
        protected virtual ValueTask InternalStartAsync() { return new ValueTask(); }
        
        /// <summary>
        /// 하위 객체에서 정의하는 종료시 호출
        /// </summary>
        protected virtual ValueTask InternalStopAsync() { return new ValueTask(); }

        public ISession SetSessionBehaviour(ISessionBehaviour bhvr)
        {
            behaviour = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this;
        }
    }
}