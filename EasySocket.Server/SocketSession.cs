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

        public virtual async ValueTask StartAsync(SessionParameter<TSession> param, Socket sck)
        {
            param = param ?? throw new ArgumentNullException(nameof(param));
            socket = sck ?? throw new ArgumentNullException(nameof(sck));

            int prevState = Interlocked.CompareExchange(ref _state, (int)SessionState.Starting, (int)SessionState.None);
            if (prevState != (int)SessionState.None)
            {
                throw new InvalidOperationException($"The session has an invalid initial state. : Session state is {(SessionState)prevState}");
            }

            if (behaviour == null)
            {
                param.logger.MemberNotSetWarn("Session Behavior", "SetSessionBehavior");
            }

            behaviour?.OnStartBefore(this);

            await InternalStartAsync();

            _state = (int)SessionState.Running;

            OnStarted();
        }
        
        public virtual async ValueTask StopAsync()
        {
            if (_state != (int)SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid state. : Session state is {(SessionState)_state}");
            }

            await ProcessStopAsync();
        }

        /// <summary>
        /// 세션이 시작된 후 외부(<see cref="IServer"/>) 계열 클래스 에서 호출되는 메서드
        /// </summary>
        protected virtual void OnStarted()
        {
            if (_state != (int)SessionState.Running)
            {
                param.logger.Error($"The session has an invalid state. : Session state is {(SessionState)_state}");
                return;
            }

            behaviour?.OnStartAfter(this);
        }

        /// <summary>
        /// 세션이 종료된 후 외부(<see cref="IServer"/>) 계열 클래스 에서 호출되는 메서드
        /// </summary>
        protected virtual void OnStopped()
        {
            if (_state != (int)SessionState.Stopped)
            {
                param.logger.Error($"The session has an invalid state. : Session state is {(SessionState)_state}");
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
                param.logger.Error($"The session has an invalid state. : Session state is {(SessionState)prevState}");
            }

            await InternalStopAsync();

            _state = (int)SessionState.Stopped;

            OnStopped();
        }

        /// <summary>
        /// 데이터 수신시 호출되는 메서드.
        /// </summary>
        protected long ProcessReceive(ReadOnlySequence<byte> sequence)
        {
            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var packet = param.msgFilter.Filter(ref sequenceReader);
                    if (packet == null)
                    {
                        return sequence.Length;
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