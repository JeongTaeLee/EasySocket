using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Buffers;
using EasySocket.Logging;
using System.IO;

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
        public override void Start(Socket sck, ILogger lgr)
        {
            base.Start(sck, lgr);

            _sendLock = new SemaphoreSlim(1, 1);
            _cancelTokenSource = new CancellationTokenSource();

            _networkStream = new NetworkStream(sck);
            _pipeReader = PipeReader.Create(_networkStream, new StreamPipeReaderOptions());

            _receiveTask = ReceiveLoop();

            WaitClose();
        }

        public override void Close()
        {
            _cancelTokenSource?.Cancel();
            InternalCloseAsync().Wait();
        }

        protected override void InternalClose()
        {
            base.InternalClose();

            _networkStream?.Close();

            _networkStream = null;
            _pipeReader = null;
            _sendLock = null;
            _cancelTokenSource = null;
        }

        public override async ValueTask CloseAsync()
        {
            _cancelTokenSource?.Cancel();
            await InternalCloseAsync();
        }

        public override int Send(ReadOnlyMemory<byte> sendMemory)
        {
            try
            {
                _sendLock.Wait();

                return socket.SendAsync(sendMemory, SocketFlags.None)
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            try
            {
                await _sendLock.WaitAsync();
                return await socket.SendAsync(sendMemory, SocketFlags.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        #endregion

        private async void WaitClose()
        {
            await InternalCloseAsync();
        }

        private async Task InternalCloseAsync()
        {
            await _receiveTask;

            if (Interlocked.CompareExchange(ref _isClose, 1, 0) != 0)
            {
                return;
            }

            onClose?.Invoke();

            InternalClose();
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