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
        private SemaphoreSlim _sendLock = null;
        private CancellationTokenSource _cancelTokenSource = null;

        private NetworkStream _networkStream = null;
        private PipeReader _pipeReader = null;

        private Task _receiveTask = null;
        
        #region BaseSocketProxy Method
        public override void Start(Socket sck, ILogger lgr)
        {
            base.Start(sck, lgr);

            _sendLock = new SemaphoreSlim(1, 1);
            _cancelTokenSource = new CancellationTokenSource();

            // 두번째 인자인 ownsSocket이 true면 NetworkStream Close 시 소켓도 같이 Close 된다.
            _networkStream = new NetworkStream(sck, true);
            _pipeReader = PipeReader.Create(_networkStream, new StreamPipeReaderOptions());

            _receiveTask = ReceiveLoop();
        }

        public override void Close()
        {
            _cancelTokenSource?.Cancel();
            
            _receiveTask?.Wait();
            
            _networkStream?.Close();
            
            InternalClose();
        }

        public override async ValueTask CloseAsync()
        {
            _cancelTokenSource?.Cancel();
            
            await (_receiveTask ?? Task.CompletedTask);

            _networkStream?.Close();

            InternalClose();
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

        private void InternalClose()
        {
            _sendLock = null;
            _cancelTokenSource = null;

            _networkStream = null;
            _pipeReader = null;

            _receiveTask = null;
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
                        if (result.IsCanceled)
                        {
                            readLength = buffer.Length;
                            break;
                        }
                        
                        if (0 < buffer.Length)
                        {
                            readLength = onReceived?.Invoke(ref buffer) ?? buffer.Length;
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
                    }
                    finally
                    {
                        _pipeReader.AdvanceTo(buffer.GetPosition(readLength));
                    }
                }
            }
            finally
            {
                await _pipeReader.CompleteAsync();
                onClose?.Invoke();
            }
        }
    }
}