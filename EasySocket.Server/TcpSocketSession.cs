using System;
using System.Threading;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace EasySocket.Server
{
    public class TcpSocketSession<TPacket> : SocketSession<TcpSocketSession<TPacket>, TPacket>
    {
        private CancellationTokenSource _cancellationTokenSource = null;

        private NetworkStream _networkStream = null;
        private PipeReader _pipeReader = null;

        private Task _receiveTask = null;

        protected override ValueTask ProcessStart()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _networkStream = new NetworkStream(socket);
            _pipeReader = PipeReader.Create(_networkStream);

            _receiveTask = ReceiveLoop();

            WaitingForAbort();

            return new ValueTask();
        }

        protected override async ValueTask ProcessStop()
        {
            await (_receiveTask ?? Task.CompletedTask);

            _networkStream?.Close();
        
            _cancellationTokenSource = null;
            _networkStream = null;
            _pipeReader = null;
            _receiveTask = null;
        }

        protected override async ValueTask<int> ProcessSend(ReadOnlyMemory<byte> mmry)
        {
            return await socket.SendAsync(mmry, SocketFlags.None);
        }

        private async void WaitingForAbort()
        {
            await _receiveTask;

            await OnStop();
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var result = await _pipeReader.ReadAsync(_cancellationTokenSource.Token);
                    var buffer = result.Buffer;

                    var readLength = 0L;

                    try
                    {
                        if (0 < buffer.Length)
                        {
                            readLength = OnReceive(ref buffer);
                        }

                        if (result.IsCanceled)
                        {
                            readLength = buffer.Length;
                            break;
                        }

                        if (result.IsCompleted)
                        {
                            readLength = buffer.Length;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        readLength = buffer.Length;
                        OnError(ex);
                        
                        break;
                    }
                    finally
                    {
                        _pipeReader.AdvanceTo(buffer.GetPosition(readLength));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // cancel~
            }
            finally
            {
                await _pipeReader.CompleteAsync();
            }
        }
    }
}