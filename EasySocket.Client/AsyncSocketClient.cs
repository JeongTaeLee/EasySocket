using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace EasySocket.Client
{
    public class AsyncSocketClient : BaseSocketClient<AsyncSocketClient>
    {
        private CancellationTokenSource _cancellation;
        private NetworkStream _networkStream = null;
        private PipeReader _pipeReader = null;
        private Task _readTask = null;
        private int _isClose = 0;

        protected override Socket CreateSocket(SocketClientConfig sckCnfg)
        {
            return new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                SendBufferSize = sckCnfg.sendBufferSize,
                ReceiveBufferSize = sckCnfg.receiveBufferSize
            };
        }

        protected override Task InternalStart()
        {
            _cancellation = new CancellationTokenSource();
            _networkStream = new NetworkStream(this.socket);
            _pipeReader = PipeReader.Create(_networkStream);

            _readTask = ReadAsync();

            WaitStopAsyncWrapper();

            return Task.FromResult(true);
        }

        protected override async Task InternalStop()
        {
            await WaitStopAsync();
        }

        private async void WaitStopAsyncWrapper()
        {
            await WaitStopAsync();
        }

        private async Task WaitStopAsync()
        {
            await _readTask;

            if (Interlocked.CompareExchange(ref _isClose, 1, 0) != 0)
            {
                return;
            }

            _networkStream.Close();
        }

        private async Task ReadAsync()
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    var result = await _pipeReader.ReadAsync(_cancellation.Token);
                    var buffer = result.Buffer;

                    var readLength = 0L;

                    try
                    {
                        if (0 < buffer.Length)
                        {
                            readLength = OnRead(ref buffer);
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
                        onError?.Invoke(this, ex);

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