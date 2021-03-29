using System;
using System.Buffers;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Extensions;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Client
{
    public abstract class BaseSocketClient<TSocketClient, TPacket> : IClient<TPacket>
        where TSocketClient : class, IClient<TPacket>
    {
        public ClientState state => (ClientState)_state;
        public IClientBehavior<TPacket> behavior { get; private set; } = null;

        private SemaphoreSlim _sendSemaphore = null;
        private int _state = (int)ClientState.None;
        
        protected ILogger _logger = null;

        public Socket socket { get; private set; } = null;
        public SocketClientConfig config { get; private set; } = null;
        public IMsgFilter<TPacket> msgFilter { get; private set; } = null;
        public ILoggerFactory loggerFactroy { get; private set; } = null;

        public async Task StartAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ClientState.Starting, (int)ClientState.None);
            if (prevState != (int)ClientState.None)
            {
                throw new InvalidOperationException($"The client is in an invalid state. : Initial state cannot be \"{(ClientState)prevState}\"");
            }

            if (msgFilter == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("MsgFilter", "SetMsgFilter");
            }

            if (loggerFactroy == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("LoggerFactory", "SetLoggerFactory");
            }

            if (config == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("SocketClientConfig", "SetSocketClientConfig");
            }

            _logger = loggerFactroy.GetLogger(GetType());
            if (_logger == null)
            {
                throw new InvalidOperationException("Unable to create logger from LoggerFactory.");
            }

            if (behavior == null)
            {
                _logger.MemberNotSetWarn("Client Behavior", "SetClientBehavior");
            }

            socket = CreateSocket(config);
            if (socket == null)
            {
                throw new InvalidOperationException("Unable to create socket.");
            }

            _sendSemaphore = new SemaphoreSlim(1, 1);

            await socket.ConnectAsync(config.ip.ToIPAddress(), config.port);
            await ProcessStart();

            _state = (int)ClientState.Running;

            behavior?.OnStarted(this as TSocketClient);
        }

        public async Task StopAsync()
        {
            await OnStop();
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            if (_state != (int)ClientState.Running)
            {
                return -1;
            }

            try
            {
                await _sendSemaphore.WaitAsync();

                return await ProcessSend(sendMemory);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// 수신한 데이터를 <see cref="IMsgInfo"/>로 변환하는 <see cref="IMsgFilter"/>를 설정합니다.
        /// </summary>
        public TSocketClient SetMsgFilter(IMsgFilter<TPacket> fltr)
        {
            msgFilter = fltr ?? throw new ArgumentNullException(nameof(fltr));
            return this as TSocketClient;
        }

        /// <summary>
        /// <see cref="SocketClientConfig"/>를 설정합니다.
        /// </summary>
        public TSocketClient SetSocketClientConfig(SocketClientConfig cnfg)
        {
            config = cnfg ?? throw new ArgumentNullException(nameof(cnfg));
            return this as TSocketClient;
        }

        /// <summary>
        /// <see cref="IClient"/>에서 발생하는 여러 이벤트들을 핸들링하는 <see cref="IClientBehavior"/>를 설정합니다.
        /// </summary>
        public TSocketClient SetClientBehavior(IClientBehavior<TPacket> bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as TSocketClient;
        }

        /// <summary>
        /// <see cref="IClient"/>에서 사용하는 <see cref="ILogger"/>을 생성하는 <see cref="ILoggerFactory"/>를 설정합니다.
        /// </summary>
        public TSocketClient SetLoggerFactory(ILoggerFactory factory)
        {
            loggerFactroy = factory ?? throw new ArgumentNullException(nameof(factory));
            return this as TSocketClient;
        }

        /// <summary>
        /// 내부에서 에러 발생 시 호출되는 함수입니다. 해당 함수 호출 후 <see cref="IClientBehavior.OnError(IClient, Exception)"/> 가 호출됩니다.
        /// </summary>
        protected virtual void OnError(Exception ex)
        {
            behavior?.OnError(this as TSocketClient, ex);
        }

        /// <summary>
        /// 내부에서 호출되는 종료 로직입니다. 내부에서 클라이언트를 종료해야할 때 해당 함수를 호출하면 됩니다.
        /// </summary>
        protected virtual async Task OnStop()
        {
            if (Interlocked.CompareExchange(ref _state, (int)ClientState.Stopping, (int)ClientState.Running) != (int)ClientState.Running)
            {
                return;
            }

            await ProcessStop();

            socket?.Dispose();
            socket = null;
            _state = (int)ClientState.Stopped;

            behavior?.OnStoped(this as TSocketClient);
        }

        /// <summary>
        /// 내부에서 읽기 이벤트 발생 시 호출되는 메서드 입니다. 파생 클래스에서 호출 후 <see cref="IMsgFilter"/>에서 데이터를 변환 후
        /// <see cref="IClientBehavior.OnReceived(IClient, IMsgFilter)"/> 호출 됩니다.
        /// </summary>
        protected virtual long OnRead(ref ReadOnlySequence<byte> sequence)
        {
            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var msgInfo = msgFilter.Filter(ref sequenceReader);

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

                OnStop().DoNotWait();

                return (int)sequence.Length;
            }
        }

        /// <summary>
        /// <see cref="BaseSocketClient{TSocketClient}"/>의 파생 클래스에서 재정의하여 사용될 소켓을 생성하여 반환합니다.
        /// </summary>
        protected abstract Socket CreateSocket(SocketClientConfig sckCnfg);

        /// <summary>
        /// <see cref="BaseSocketClient{TSocketClient}"/>의 파생 클래스에서 재정의하여 시작될 때 수행할 행동을 구현합니다.
        /// </summary>
        protected abstract Task ProcessStart();

        /// <summary>
        /// <see cref="BaseSocketClient{TSocketClient}"/>의 파생 클래스에서 재정의하여 종료될 때 수행할 행동을 구현합니다.
        /// 해당 메서드는 파생 클래스에서 따로 호출되지 않는 이상 종료될 때 단 한번 호출을 보장합니다. 
        /// </summary>
        protected abstract Task ProcessStop();
        protected abstract ValueTask<int> ProcessSend(ReadOnlyMemory<byte> sendMemory);
    }
}