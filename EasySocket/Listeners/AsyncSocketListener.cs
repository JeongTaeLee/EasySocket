using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using EasySocket.Workers;
using EasySocket.Logging;

namespace EasySocket.Listeners
{
    public class AsyncSocketListener : BaseListener
    {        
        private Socket _listenSocket = null;
        private Task _acceptLoopTask = null; 
        private CancellationTokenSource _acceptLoopCanelToken = null;

#region BaseListener Method
        public override void Start(ListenerConfig config, ILogger logger)
        {
            base.Start(config, logger);

            _acceptLoopCanelToken = new CancellationTokenSource();

            IPEndPoint endPoint = new IPEndPoint(ParseAddress(this.config.ip), this.config.port);

            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(this.config.backlog);
            _listenSocket.NoDelay = config.listenerNoDelay;

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
#endregion

        private async Task AcceptLoop()
        {
            while (!_acceptLoopCanelToken.IsCancellationRequested)
            {
                try
                {
                    var acceptedSocket = await _listenSocket.AcceptAsync().ConfigureAwait(false);

                    if (acceptedSocket == null)
                    {
                        break;
                    }

                    if (_acceptLoopCanelToken.IsCancellationRequested)
                    {
                        break;
                    }

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