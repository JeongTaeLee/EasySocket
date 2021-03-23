using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Client
{
    public abstract class BaseSocketClient<TSocketClient> : ISocketClient<TSocketClient>
        where TSocketClient : class, ISocketClient<TSocketClient>
    {
        public bool isClose => (_isClose == 1);
        public IClientBehavior behavior { get; private set; } = null;

        private int _isClose = 0;
        public Socket socket { get; private set; } = null;
        public SocketClientConfig config { get; private set; } = null;


        public void Start()
        {
            if (config == null)
            {
                throw new InvalidOperationException("SocketClientConfig is not set : Please call the \"SetSocketClientConfig\" Method and set it up.");
            }

            socket = CreateSocket(config);
            if (socket == null)
            {
                throw new InvalidOperationException("Unable to create socket.");
            }

            socket.ConnectAsync(ParseAddress(config.ip), config.port).Wait();
            
            InternalStart().Wait();
        }

        public async Task StartAsync()
        {
            if (config == null)
            {
                throw new InvalidOperationException("SocketClientConfig is not set : Please call the \"SetSocketClientConfig\" Method and set it up.");
            }

            socket = CreateSocket(config);
            if (socket == null)
            {
                throw new InvalidOperationException("Unable to create socket.");
            }

            await socket.ConnectAsync(ParseAddress(config.ip), config.port);
            await InternalStart();

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
            throw new NotImplementedException();
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            throw new NotImplementedException();
        }

        public TSocketClient SetSocketClientConfig(SocketClientConfig cnfg)
        {
            config = cnfg ?? throw new ArgumentNullException(nameof(cnfg));
            return this as TSocketClient;
        }

        public TSocketClient SetClientBehavior(IClientBehavior bhvr)
        {
            behavior = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
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
            if (isClose)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _isClose, 1, 0) != 0)
            {
                return;
            }

            await InternalStop();

            behavior?.OnStoped(this as TSocketClient);
        }

        /// <summary>
        /// 내부에서 읽기 이벤트 발생 시 호출되는 메서드 입니다. 파생 클래스에서 호출 후 <see cref="IMsgFilter"/>에서 데이터를 변환 후
        /// <see cref="IClientBehavior.OnReceived(IClient, IMsgFilter)"/> 호출 됩니다.
        /// </summary>
        protected virtual long OnRead(ref ReadOnlySequence<byte> sequence)
        {
            var readLength = sequence.Length;

            behavior?.OnReceived(this as TSocketClient, null);

            return readLength;
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