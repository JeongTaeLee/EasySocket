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
        private CancellationTokenSource _cancelToken = null;
        private NetworkStream _networkStream = null;
        private Task _receiveLoopTask = null;

#region BaseSocketProxy Method
        public override void Start(Socket socket, ILogger logger)
        {
            base.Start(socket, logger);
            
            _cancelToken = new CancellationTokenSource();
            _networkStream = new NetworkStream(socket);
            _receiveLoopTask = ReceiveLoop();
        }

        public override void Close()
        {
            CloseAsync().GetAwaiter().GetResult();
        }

        public override async ValueTask CloseAsync()
        {
            _cancelToken.Cancel();
            _networkStream.Close();

            await _receiveLoopTask;

            _cancelToken = null;
            _networkStream = null;
            _receiveLoopTask = null;
        }

        public override int Send(ReadOnlyMemory<byte> sendMemory)
        {
            return socket.SendAsync(sendMemory, SocketFlags.None)
                .GetAwaiter()
                .GetResult();
        }

        public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            return await socket.SendAsync(sendMemory, SocketFlags.None);
        }
#endregion

        private async Task ReceiveLoop()
        {
            var reader = PipeReader.Create(_networkStream);
  
            while (_cancelToken.IsCancellationRequested)
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