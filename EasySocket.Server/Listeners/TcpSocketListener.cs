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
        private Task _acceptLoopTask = null;
        
        protected override ValueTask InternalStartAsync()
        {
            IPEndPoint endPoint = new IPEndPoint(config.ip.ToIPAddress(), config.port);

            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(this.config.backlog);

            _acceptLoopTask = AcceptLoop();

            return new ValueTask();
        }

        protected override async ValueTask InternalStopAsync()
        {
            _listenSocket?.Close(); // Accept socket 은 SafeClose 호출 시 에러(연결이 안되어 있기 때문에)
            
            await _acceptLoopTask;

            _listenSocket = null;
            _acceptLoopTask = null;
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
                        break;
                    }

                    ProcessAccept(acceptedSocket);
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

                    ProcessError(ex);
                }
                catch (Exception ex)
                {
                    // 예측하지 못한 오류.
                    ProcessError(ex);
                }
            }
        }
    }
}