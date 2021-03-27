using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using EasySocket.Common.Logging;
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

                    accepted?.Invoke(this, acceptedSocket);
                }
                catch (ObjectDisposedException)
                {
                    // 종료된 소켓으로 다시 Accepte 호출 할 때.
                    break;
                }
                catch (SocketException ex)
                {
                    // 소켓 종료, .. 오류에서 제외할 코드 있으면 추가하기
                    if (ex.ErrorCode == 89)
                        break;

                    error?.Invoke(this, ex);
                }
                catch (Exception ex)
                {
                    // 예측하지 못한 오류.
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