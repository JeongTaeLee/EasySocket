using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO.Pipelines;
using EasySocket.Common.Logging;


namespace EasySocket.SocketProxys
{
    public class AsyncSocketProxy : BaseSocketProxy
    {
        private SemaphoreSlim _sendLock = null;
        private CancellationTokenSource _cancelTokenSource = null;

        private NetworkStream _networkStream = null;
        private PipeReader _pipeReader = null;

        private Task _receiveTask = null;

        private int _isClose = 0;

        #region BaseSocketProxy Method
        public override void Start(Socket sckt, ILogger lger)
        {
            base.Start(sckt, lger);

            _sendLock = new SemaphoreSlim(1, 1);
            _cancelTokenSource = new CancellationTokenSource();

            _networkStream = new NetworkStream(sckt);
            _pipeReader = PipeReader.Create(_networkStream, new StreamPipeReaderOptions());

            _receiveTask = ReceiveLoop();

            WaitStopAsyncWrapper();
        }

        public override void Stop()
        {
            _cancelTokenSource?.Cancel();
            WaitStopAsync().Wait();
        }

        public override async Task StopAsync()
        {
            _cancelTokenSource?.Cancel();
            await WaitStopAsync();
        }

        protected override void OnStop()
        {
            base.OnStop();

            _networkStream?.Close();

            _networkStream = null;
            _pipeReader = null;
            _sendLock = null;
            _cancelTokenSource = null;
        }

        public override int Send(ReadOnlyMemory<byte> sendMmry)
        {
            try
            {
                _sendLock.Wait();

                return socket.SendAsync(sendMmry, SocketFlags.None)
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMmry)
        {
            try
            {
                await _sendLock.WaitAsync();
                return await socket.SendAsync(sendMmry, SocketFlags.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        #endregion

        private async void WaitStopAsyncWrapper()
        {
            await WaitStopAsync();
        }

        private async Task WaitStopAsync()
        {
            await _receiveTask;

            if (Interlocked.CompareExchange(ref _isClose, 1, 0) != 0)
            {
                return;
            }

            onClose?.Invoke();

            OnStop();
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (!_cancelTokenSource.IsCancellationRequested)
                {
                    var result = await _pipeReader.ReadAsync(_cancelTokenSource.Token);
                    var buffer = result.Buffer;
                    
                    var readLength = 0L;
                    
                    try
                    {
                        if (0 < buffer.Length)
                        {
                            readLength = onReceived?.Invoke(ref buffer) ?? buffer.Length;
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
                        onError?.Invoke(ex);

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