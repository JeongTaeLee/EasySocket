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
                behaviour?.OnStartBefore(this);

                await InternalStartAsync();
                _state = (int)SessionState.Running;

                OnStarted();
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
        protected virtual void OnStarted()
        {
            if (state != SessionState.Running)
            {
                ExceptionExtensions.InvalidObjectStateIOE("Session", state);
                return;
            }

            behaviour?.OnStartAfter(this);
        }

        /// <summary>
        /// 세션이 종료된 후 외부(<see cref="IServer"/>) 계열 클래스 에서 호출되는 메서드
        /// </summary>
        protected virtual void OnStopped()
        {
            if (state != SessionState.Stopped)
            {
                ExceptionExtensions.InvalidObjectStateIOE("Session", state);
                return;
            }

            behaviour?.OnStopped(this);
        }

        /// <summary>
        /// 내부에서 사용하는 종료 메서드.
        /// </summary>
        protected async ValueTask ProcessStopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Stopping, (int)SessionState.Running);
            if (prevState != (int)SessionState.Running)
            {
                ExceptionExtensions.InvalidObjectStateIOE("Session", (SessionState)prevState);
                return;
            }
            
            socket?.Close();

            await InternalStopAsync();

            _state = (int)SessionState.Stopped;

            OnStopped();

            param?.onStop?.Invoke(this as TSession);
        }

        /// <summary>
        /// 데이터 수신시 호출되는 메서드.
        /// </summary>
        protected long ProcessReceive(ReadOnlySequence<byte> sequence)
        {
            if (state != SessionState.Running)
            {
                ExceptionExtensions.InvalidObjectStateIOE("Session", state);
            }

            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var packet = param.msgFilter.Filter(ref sequenceReader);
                    if (packet == null)
                    {
                        break;
                    }

                    behaviour?.OnReceived(this, packet).GetAwaiter().GetResult(); // 대기
                }

                return (int)sequenceReader.Consumed;
            }
            catch (Exception ex)
            {
                ProcessError(ex);
                return sequence.Length;
            }
        }

        /// <summary>
        /// 내부 오류 발생시 호출되는 메서드.
        /// </summary>
        protected void ProcessError(Exception ex)
        {
            behaviour?.OnError(this as TSession, ex);
        }

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