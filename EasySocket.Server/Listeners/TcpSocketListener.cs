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
        
        protected override ValueTask InternalStart()
        {
            IPEndPoint endPoint = new IPEndPoint(config.ip.ToIPAddress(), config.port);

            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(this.config.backlog);

            _cancellationToken = new CancellationTokenSource();
            _acceptLoopTask = AcceptLoop(_cancellationToken.Token);

            return ValueTask.CompletedTask;
        }

        protected override async ValueTask InternalStop()
        {
            _cancellationToken?.Cancel();
            
            await _acceptLoopTask;
            _acceptLoopTask = null;

            _listenSocket.SafeClose();
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
                catch (ObjectDisposedException ex)
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