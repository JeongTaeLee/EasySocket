using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Buffers;

namespace EasySocket.SocketProxys
{
    public class AsyncSocketProxy : BaseSocketProxy
    {
        private CancellationTokenSource _receiveLoopCanelToken = null;
        private Task receiveLoopTask = null;

        public override void Start()
        {
            base.Start();
            
            _receiveLoopCanelToken = new CancellationTokenSource();

            receiveLoopTask = ReceiveLoop();
        }

        public override void Stop()
        {
            _receiveLoopCanelToken.Cancel();
            
            socket.Close();

            _receiveLoopCanelToken = null;
        }

        private async Task ReceiveLoop()
        {
            var networkStream = new NetworkStream(socket);
            var reader = PipeReader.Create(networkStream);
  
            while (_receiveLoopCanelToken.IsCancellationRequested)
            {
                try
                {
                    ReadResult result = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    // TODO @jeongtae.lee : 수신 로직 구현.
                    int readLength = received.Invoke(ref buffer);
                    reader.AdvanceTo(buffer.GetPosition(readLength, buffer.Start), buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    error?.Invoke(ex);
                }
            }
        }
    }
}