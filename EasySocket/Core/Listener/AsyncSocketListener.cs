using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace EasySocket.Core.Listener
{
    public class AsyncSocketListener : IListener
    {
        public ListenerConfig config { get; private set;}

        public event ListenerAcceptHandler accepted;
        public event ListenerErrorHandler error;

        private Socket _listenSocket = null;
        private Task _acceptedLoopTask = null; 

        private CancellationTokenSource acceptLoopCanelToken = new CancellationTokenSource();

        public void Start(ListenerConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.ip))
            {
                throw new ArgumentNullException(nameof(config.ip));
            }

            if (0 > config.port || short.MaxValue < config.port){
                throw new ArgumentException("Invalid Port Range");
            }

            this.config = config;            

            IPEndPoint endPoint = new IPEndPoint(ParseAddress(this.config.ip), this.config.port);

            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(this.config.backlog);

            _acceptedLoopTask = AcceptLoop();
        }

        public void Close()
        {
            acceptLoopCanelToken.Cancel();
            _listenSocket.Close();
             
            _acceptedLoopTask.Wait();
        }

        private async Task AcceptLoop()
        {
            while (true)
            {
                var acceptedSocket = await _listenSocket.AcceptAsync().ConfigureAwait(false);

                if (acceptLoopCanelToken.IsCancellationRequested == true)
                {
                    return;
                }
 
                acceptedSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, true);

                accepted?.Invoke(this, acceptedSocket);               
            }
        }

        private IPAddress ParseAddress(string strIp)
        {
            if (strIp == "Any")
            {
                return IPAddress.Any;
            }

            return IPAddress.Parse(strIp);
        }
    }
}