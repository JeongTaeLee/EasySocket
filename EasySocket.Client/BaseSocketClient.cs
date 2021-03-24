using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Protocols.MsgInfos;
using EasySocket.Common.Protocols.MsgFilters;
using EasySocket.Common.Logging;

namespace EasySocket.Client
{
    public abstract class BaseSocketClient<TSocketClient> : ISocketClient<TSocketClient>
        where TSocketClient : class, ISocketClient<TSocketClient>
    {
        public IClient.State state => (IClient.State)_state;
        public IMsgFilter msgFilter { get; private set; } = null;
        public IClientBehavior behavior { get; private set; } = null;
        public ILoggerFactory loggerFactroy { get; private set; } = null;
        public SocketClientConfig config { get; private set; } = null;
        public Socket socket { get; private set; } = null;

        private SemaphoreSlim _sendSemaphore = null;
        private int _state = (int)IClient.State.None;
        protected ILogger _logger = null;


        public void Start()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)IClient.State.Starting, (int)IClient.State.None);
            if (prevState != (int)IClient.State.None)
            {
                throw new InvalidOperationException($"The client is in an invalid state. : Initial state cannot be \"{(IClient.State)prevState}\"");
            }

            if (msgFilter == null)
            {
                throw new InvalidOperationException("MsgFilter not set: Please call the \"SetMsgFilter\" Method and set it up.");
            }

            if (behavior == null)
            {
                _logger.Warn("Client Behavior is not set. : Unable to receive events for the client. Please call the \"SetClientBehavior\" Method and set it up.");
            }

            if (loggerFactroy == null)
            {
                throw new InvalidOperationException("ILoggerFactory not set: Please call the \"SetLoggerFactory\" Method and set it up.");
            }

            _logger = loggerFactroy.GetLogger(GetType());
            if (_logger == null)
            {
                throw new InvalidOperationException("Unable to create logger from LoggerFactory.");
            }

            if (config == null)
            {
                throw new InvalidOperationException("SocketClientConfig is not set : Please call the \"SetSocketClientConfig\" Method and set it up.");
            }

            _sendSemaphore = new SemaphoreSlim(1, 1);

            socket = CreateSocket(config);
            if (socket == null)
            {
                throw new InvalidOperationException("Unable to create socket.");
            }

            socket.ConnectAsync(ParseAddress(config.ip), config.port).Wait();            
            InternalStart().Wait();

            behavior?.OnStarted(this as TSocketClient);

            _state = (int)IClient.State.Running;
        }

        public async Task StartAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)IClient.State.Starting, (int)IClient.State.None);
            if (prevState != (int)IClient.State.None)
            {
                throw new InvalidOperationException($"The client is in an invalid state. : Initial state cannot be \"{(IClient.State)prevState}\"");
            }

            if (msgFilter == null)
            {
                throw new InvalidOperationException("MsgFilter not set: Please call the \"SetMsgFilter\" Method and set it up.");
            }

            if (behavior == null)
            {
                _logger.Warn("Client Behavior is not set. : Unable to receive events for the client. Please call the \"SetClientBehavior\" Method and set it up.");
            }

            if (loggerFactroy == null)
            {
                throw new InvalidOperationException("ILoggerFactory not set: Please call the \"SetLoggerFactory\" Method and set it up.");
            }

            _logger = loggerFactroy.GetLogger(GetType());
            if (_logger == null)
            {
                throw new InvalidOperationException("Unable to create logger from LoggerFactory.");
            }

            if (config == null)
            {
                throw new InvalidOperationException("SocketClientConfig is not set : Please call the \"SetSocketClientConfig\" Method and set it up.");
            }

            socket = CreateSocket(config);
            if (socket == null)
            {
                throw new InvalidOperationException("Unable to create socket.");
            }

            _sendSemaphore = new SemaphoreSlim(1, 1);

            await socket.ConnectAsync(ParseAddress(config.ip), config.port);
            await InternalStart();

            _state = (int)IClient.State.Running;

            behavior?.OnStarted(this as TSocketClient);
        }

        public void Stop()
        {
            OnStop().Wait();
        }

        public async Task StopAsync()
        {
            await OnStop();
        }

        public int Send(ReadOnlyMemory<byte> sendMemory)
        {
            if (_state != (int)IClient.State.Running)
            {
                return -1;
            }

            try
            {
                _sendSemaphore.Wait();

                return InternalSendSync(sendMemory);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            if (_state != (int)IClient.State.Running)
            {
                return -1;
            }

            try
            {
                await _sendSemaphore.WaitAsync();

                return await InternalSendAsync(sendMemory);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// 수신한 데이터를 <see cref="IMsgInfo"/>로 변환하는 <see cref="IMsgFilter"/>를 설정합니다.
        /// </summary>
        public TSocketClient SetMsgFilter(IMsgFilter fltr)
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
        public TSocketClient SetClientBehavior(IClientBehavior bhvr)
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
            if (Interlocked.CompareExchange(ref _state, (int)IClient.State.Stopping, (int)IClient.State.Running) != (int)IClient.State.Running)
            {
                return;
            }

            await InternalStop();

            socket?.Dispose();
            socket = null;
            _state = (int)IClient.State.Stopped;

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
                throw new Exception("Test");

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

                OnStop();

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
        protected abstract Task InternalStart();

        /// <summary>
        /// <see cref="BaseSocketClient{TSocketClient}"/>의 파생 클래스에서 재정의하여 종료될 때 수행할 행동을 구현합니다.
        /// 해당 메서드는 파생 클래스에서 따로 호출되지 않는 이상 종료될 때 단 한번 호출을 보장합니다. 
        /// </summary>
        protected abstract Task InternalStop();
        protected abstract int InternalSendSync(ReadOnlyMemory<byte> sendMemory);
        protected abstract ValueTask<int> InternalSendAsync(ReadOnlyMemory<byte> sendMemory);

        protected IPAddress ParseAddress(string strIp)
        {
            if (strIp == "Any")
            {
                return IPAddress.Any;
            }

            return IPAddress.Parse(strIp);
        }

    }
}