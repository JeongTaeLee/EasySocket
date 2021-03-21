using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using EasySocket.Servers;
using EasySocket.Logging;

namespace EasySocket.Listeners
{
    public class AsyncSocketListener : BaseListener
    {
        private CancellationTokenSource _cancellationToken = null;
        private Socket _listenSocket = null;
        private Task _acceptLoopTask = null; 

#region BaseListener Method
        public override void Start(ListenerConfig cnfg, ILogger lger)
        {
            base.Start(cnfg, lger);

            _cancellationToken = new CancellationTokenSource();

            IPEndPoint endPoint = new IPEndPoint(ParseAddress(this.config.ip), this.config.port);

            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(this.config.backlog);

            _acceptLoopTask = AcceptLoop();
        }

        public override void Stop()
        {
            _cancellationToken?.Cancel();
            _listenSocket?.Close();
             
            _acceptLoopTask?.Wait();

            _cancellationToken = null;
            _listenSocket = null;
            _acceptLoopTask = null;
        }

        public override async Task StopAsync()
        {
            _cancellationToken?.Cancel();
            _listenSocket?.Close();

            await _acceptLoopTask;

            _cancellationToken = null;
            _listenSocket = null;
            _acceptLoopTask = null;
        }
#endregion

        private async Task AcceptLoop()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var acceptedSocket = await _listenSocket.AcceptAsync().ConfigureAwait(false);

                    if (_cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (acceptedSocket == null)
                    {
                        break;
                    }

                    accepted?.Invoke(this, acceptedSocket);
                }
                catch (SocketException ex)
                {
                    error?.Invoke(this, ex);
                }
                catch (ObjectDisposedException ex)
                {
                    error?.Invoke(this, ex);
                    break;
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