using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Client
{
    public abstract class SocketClient<TClient> : IClient
        where TClient : SocketClient<TClient>
    {
        public ClientState state => (ClientState)_state;
        public IClientBehaviour behaviour { get; private set; } = null;

        private int _state = (int)ClientState.None;

        protected Socket socket { get; private set; } = null;

        public ILogger logger { get; private set; } = null;
        public IMsgFilter msgFilter { get; private set; } = null;
        public SocketClientConfig socketClientConfig { get; private set; } = new SocketClientConfig();

        public async Task StartAsync(string ip, int port)
        {
            if (state == ClientState.Stopped)
            {
                throw new TerminatedObjectException("Client");
            }

            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (0 > port)
            {
                throw new ArgumentException("Invalid Port Range.");
            }

            if (logger == null)
            {
                throw new MemberNotSetException(nameof(logger), nameof(SetLogger));
            }

            if (msgFilter == null)
            {
                throw new MemberNotSetException(nameof(msgFilter), nameof(SetMsgFilter));
            }

            int prevState = Interlocked.CompareExchange(ref _state, (int)ClientState.Starting, (int)ClientState.None);
            if (prevState != (int)ClientState.None)
            {
                throw new InvalidObjectStateException("Client", ((ClientState)prevState).ToString());
            }
        
            try
            {
                socket = CreateSocket(socketClientConfig);
                await socket.ConnectAsync(ip.ToIPAddress(), port);
                await InternalStartAsync();

                _state = (int)ClientState.Running;

                await OnStartedAsync();
            }
            finally
            {
                if (state != ClientState.Running)
                {
                    _state = (int)ClientState.None;
                }
            }
        }

        public async Task StopAsync()
        {
            if (state != ClientState.Running)
            {
                throw new InvalidObjectStateException("Client", state.ToString());
            }
            
            await ProcessStopAsync();
        }

        protected virtual async ValueTask OnStartedAsync()
        {
            if (state != ClientState.Running)
            {
                throw new InvalidObjectStateException("Client", state.ToString());
            }

            if (behaviour != null)
            {
                await behaviour.OnStartedAsync(this);
            }
        }

        protected virtual async ValueTask OnStoppedAsync()
        {
            if (state != ClientState.Stopped)
            {
                throw new InvalidObjectStateException("Client", state.ToString());
            }

            if (behaviour != null)
            {
                await behaviour.OnStoppedAsync(this);
            }
        }

        protected async ValueTask ProcessStopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ClientState.Stopping, (int)ClientState.Running);
            if (prevState != (int)ClientState.Running)
            {
                logger.InvalidObjectStateError("Client", (ClientState)prevState); 
                return;
            }
        
            socket?.SafeClose();

            await InternalStopAsync();

            _state = (int)ClientState.Stopped;

            await OnStoppedAsync();
        }

        protected long ProcessReceive(ReadOnlySequence<byte> sequence)
        {
            if (state != ClientState.Running)
            {
                throw new InvalidObjectStateException("Client", state.ToString());
            }

            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var packet = msgFilter.Filter(ref sequenceReader);
                    if (packet == null)
                    {
                        break;
                    }

                    behaviour?.OnReceivedAsync(this, packet).GetAwaiter().GetResult(); // 대기
                }

                return (int)sequenceReader.Consumed;
            }
            catch (Exception ex)
            {
                ProcessError(ex);
                return sequence.Length;
            }
        }

        protected void ProcessError(Exception ex)
        {
            behaviour.OnError(this, ex);
        }
        
        protected abstract Socket CreateSocket(SocketClientConfig sckCnfg);
        public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
        protected virtual ValueTask InternalStartAsync() { return new ValueTask(); }
        protected virtual ValueTask InternalStopAsync() { return new ValueTask(); }

        #region Getter/Setter
        public TClient SetMsgFilter(IMsgFilter msgFltr)
        {
            msgFilter = msgFltr ?? throw new ArgumentNullException(nameof(msgFltr));
            return this as TClient;
        }

        public TClient SetMsgFilter<TMsgFilter>()
            where TMsgFilter : IMsgFilter, new()
        {
            msgFilter = new TMsgFilter();
            return this as TClient;
        }

        public TClient SetLogger(ILogger lger)
        {
            logger = lger ?? throw new ArgumentNullException(nameof(lger));
            return this as TClient;
        }

        public TClient SetClientBehaviour(IClientBehaviour bhvr)
        {
            behaviour = bhvr ?? throw new ArgumentNullException(nameof(bhvr));
            return this as TClient;
        }

        public TClient SetSocketClientConfig(SocketClientConfig cnfg)
        {
            socketClientConfig = cnfg ?? throw new ArgumentNullException(nameof(cnfg));
            return this as TClient;
        }
        #endregion Getter/Setter
    }
}