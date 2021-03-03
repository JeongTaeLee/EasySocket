using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using EasySocket.Workers;
using System.Buffers;

namespace EasySocket.SocketProxys
{
    public class AsyncSocketProxy : BaseSocketProxy
    {
        private CancellationTokenSource _receiveLoopCanelToken = null;
        private Memory<byte> recvBuffer;
        private int offset = 0;

        public AsyncSocketProxy(Socket socket, Memory<byte> recvBuffer)
            : base(socket)
        {
            this.recvBuffer = recvBuffer;
        }

        public override void Start()
        {
            _receiveLoopCanelToken = new CancellationTokenSource();
        }

        public override void Stop()
        {
            
        }

        private async Task ReceiveLoop()
        {
            while (!_receiveLoopCanelToken.IsCancellationRequested)
            {
                int recvCount = await socket.ReceiveAsync(recvBuffer, SocketFlags.None);

                var readonlySequence = new ReadOnlySequence<byte>(recvBuffer);

                if (received == null)
                {
                    continue;
                }

            }
        }
    }
}