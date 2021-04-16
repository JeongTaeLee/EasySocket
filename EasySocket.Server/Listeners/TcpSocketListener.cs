using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;

namespace EasySocket.Server.Listeners
{
    public class TcpSocketListener : BaseListener
    {
        private Socket _listenSocket = null;
        private CancellationTokenSource _cancellationToken = null;
        private Task _acceptLoopTask = null;
        
        protected override ValueTask ProcessStart()
        {
            IPEndPoint endPoint = new IPEndPoint(config.ip.ToIPAddress(), config.port);

            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(this.config.backlog);

            _cancellationToken = new CancellationTokenSource();
            _acceptLoopTask = AcceptLoop(_cancellationToken.Token);

            return new ValueTask();
        }

        protected override async ValueTask ProcessStop()
        {
            _cancellationToken?.Cancel();
            _listenSocket?.Close(); // Accept socket 은 SafeClose 호출 시 에러(연결이 안되어 있기 때문에)
            
            await _acceptLoopTask;

            _cancellationToken = null;
            _listenSocket = null;
            _acceptLoopTask = null;
        }

        private async Task AcceptLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var acceptedSocket = await _listenSocket.AcceptAsync().ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    OnAccept(acceptedSocket);
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

                    OnError(ex);
                }
                catch (Exception ex)
                {
                    // 예측하지 못한 오류.
                    OnError(ex);
                }
            }
        }
    }
}