using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public abstract class BaseSocketClient<TSocketClient> : ISocketClient
        where TSocketClient : class
    {
        public Socket socket { get; private set; } = null;
        public SocketClientConfig config { get; private set; } = null;
        public ClientErrorHandler onError { get; set; } = null;
        public ClientReceiveHandler onReceived { get; set; } = null;

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
        }

        public void Stop()
        {
            InternalStop().Wait();
        }

        public async Task StopAsync()
        {
            await InternalStop();
        }

        public int Send(ReadOnlyMemory<byte> sendMemory)
        {
            throw new NotImplementedException();
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            throw new NotImplementedException();
        }

        public TSocketClient SetSocketClientConfig(SocketClientConfig sckCnfg)
        {
            config = sckCnfg ?? throw new ArgumentNullException(nameof(sckCnfg));
            return this as TSocketClient;
        }

        protected virtual long OnRead(ref ReadOnlySequence<byte> sequence)
        {
            return onReceived?.Invoke(this, ref sequence) ?? sequence.Length;
        }

        protected abstract Socket CreateSocket(SocketClientConfig sckCnfg);
        protected abstract Task InternalStart();
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