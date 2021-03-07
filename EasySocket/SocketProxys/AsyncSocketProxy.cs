using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Buffers;
using EasySocket.Logging;

namespace EasySocket.SocketProxys
{
    public class AsyncSocketProxy : BaseSocketProxy
    {
        private CancellationTokenSource _receiveLoopCanelToken = null;
        private Task _receiveLoopTask = null;
        private NetworkStream _networkStream = null;

#region BaseSocketProxy Method
        public override void Start(Socket socket, ILogger logger)
        {
            base.Start(socket, logger);
            
            _receiveLoopCanelToken = new CancellationTokenSource();
            _networkStream = new NetworkStream(socket);
            _receiveLoopTask = ReceiveLoop();
        }

        public override void Stop()
        {
            _receiveLoopCanelToken.Cancel();

            _networkStream.Close();

            _receiveLoopCanelToken = null;
        }
#endregion

        private async Task ReceiveLoop()
        {
            var reader = PipeReader.Create(_networkStream);
  
            while (_receiveLoopCanelToken.IsCancellationRequested)
            {
                try
                {
                    ReadResult result = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    // TODO @jeongtae.lee : 수신 로직 구현.
                    long readLength = received.Invoke(ref buffer);
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