using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using EasySocket.Workers;


namespace EasySocket.Listeners
{
    public class AsyncSocketListener : BaseListener
    {        
        private Socket _listenSocket = null;
        private Task _acceptLoopTask = null; 
        private CancellationTokenSource _acceptLoopCanelToken = null;

        public AsyncSocketListener(IServerWorker server)
            :base(server)
        {
        }

        public override void Start(ListenerConfig config)
        {
            base.Start(config);

            _acceptLoopCanelToken = new CancellationTokenSource();

            IPEndPoint endPoint = new IPEndPoint(ParseAddress(this.config.ip), this.config.port);

            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(this.config.backlog);

            _acceptLoopTask = AcceptLoop();
        }

        public override void Close()
        {
            base.Close();

            _acceptLoopCanelToken.Cancel();
            _listenSocket.Close();
             
            _acceptLoopTask.Wait();

            _listenSocket = null;
            _acceptLoopTask = null;
            _acceptLoopCanelToken = null;
        }

        private async Task AcceptLoop()
        {
            while (true)
            {
                try
                {
                    var acceptedSocket = await _listenSocket.AcceptAsync().ConfigureAwait(false);

                    if (acceptedSocket == null)
                    {
                        return;
                    }

                    if (_acceptLoopCanelToken.IsCancellationRequested == true)
                    {
                        return;
                    }

                    acceptedSocket.NoDelay = true;
 
                    accepted?.Invoke(this, acceptedSocket);               
                }
                catch (Exception ex)
                {
                    error?.Invoke(this, ex);
                }
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