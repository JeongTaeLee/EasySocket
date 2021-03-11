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
        private CancellationTokenSource _cancellationTokenSource = null;

        private NetworkStream _networkStream = null;
        private PipeReader _pipeReader = null;

        private Task _sendTask = null;
        private Task _receiveTask = null;


        #region BaseSocketProxy Method
        public override void Start(Socket socket, ILogger logger)
        {
            base.Start(socket, logger);

            _sendLock = new SemaphoreSlim(0, 1);
            _cancellationTokenSource = new CancellationTokenSource();

            _networkStream = new NetworkStream(socket);
            _pipeReader = PipeReader.Create(_networkStream, new StreamPipeReaderOptions());

            _receiveTask = ReceiveLoop();
        }

        public override void Close()
        {
            _networkStream.Close();

            _receiveTask.Wait();

            _sendLock = null;
            _networkStream = null;
            _receiveTask = null;
        }

        public override async ValueTask CloseAsync()
        {
            _networkStream.Close();

            await _receiveTask;

            _sendLock = null;
            _networkStream = null;
            _receiveTask = null;
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

        private async Task ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    var result = await _pipeReader.ReadAsync(_cancellationTokenSource.Token);
                    var buffer = result.Buffer;

                    long readLen = 0;

                    try
                    {
                        if (result.IsCanceled)
                        {
                            readLen = buffer.Length;
                            break;
                        }

                        readLen = received?.Invoke(ref buffer) ?? buffer.Length;

                        if (result.IsCompleted)
                        {
                            readLen = buffer.Length;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        readLen = buffer.Length;
                        error?.Invoke(ex);
                    }
                    finally
                    {
                        _pipeReader.AdvanceTo(buffer.GetPosition(readLen));
                    }
                }
            }
            finally
            {
                await _pipeReader.CompleteAsync();
            }
        }
    }
}