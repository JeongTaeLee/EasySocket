﻿using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols;

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
                //
                socket = CreateSocket(socketClientConfig);
                await socket.ConnectAsync(ip.ToIPAddress(), port).ConfigureAwait(false);

                //
                await InternalStartAsync();

                _state = (int)ClientState.Running;

                //
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

        protected virtual async Task OnStartedAsync()
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

        protected virtual async Task OnStoppedAsync()
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

        protected async Task ProcessStopAsync()
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

        protected async Task<ReadOnlySequence<byte>> ProcessReceive(ReadOnlySequence<byte> sequence)
        {
            if (state != ClientState.Running)
            {
                throw new InvalidObjectStateException("Client", state.ToString());
            }

            try
            {
                while (sequence.Length > 0)
                {
                    var packet = msgFilter.Filter(ref sequence);
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
            catch (Exception ex)
            {
                ProcessError(ex);
                sequence = sequence.Slice(sequence.Length);
                return sequence;
            }
        }

        protected void ProcessError(Exception ex)
        {
            behaviour.OnError(this, ex);
        }

        public abstract int Send(byte[] buffer);
        public abstract int Send(ArraySegment<byte> segment);
        public abstract Task<int> SendAsync(byte[] buffer);
        public abstract Task<int> SendAsync(ArraySegment<byte> segment);

        protected abstract Socket CreateSocket(SocketClientConfig sckCnfg);
        protected virtual Task InternalStartAsync() { return Task.CompletedTask; }
        protected virtual Task InternalStopAsync() { return Task.CompletedTask; }

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