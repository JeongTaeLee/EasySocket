using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using EasySocket.Common;

namespace EasySocket.Client
{
    public abstract class PipeSocketClient<TClient> : SocketClient<TClient>
        where TClient : SocketClient<TClient> 
    {
        private PipeWriter _pipeWriter = null;
        private PipeReader _pipeReader = null;

        protected Task writeTask { get; private set; } = null;
        protected Task readTask { get; private set; } = null;

        protected PipeWriter pipeWriter => _pipeWriter;
        protected PipeReader pipeReader => _pipeReader;
    
    
        protected override async ValueTask InternalStartAsync()
        {
            await StartPipe(out _pipeWriter, out _pipeReader);

            writeTask = WriteAsync(pipeWriter);
            readTask = ReadAsync(pipeReader);
        }

        protected override async ValueTask InternalStopAsync()
        {
            await StopPipe();

            if (writeTask != null)
            {
                await writeTask;
            }

            if (readTask != null)
            {
                await readTask;
            }

            writeTask = null;
            readTask = null;
            _pipeWriter = null;
            _pipeReader = null;
        }
        protected override void OnStarted()
        {
            if (state != ClientState.Running)
            {
                throw new InvalidObjectStateException("Client", state.ToString());
            }

            WaitingForAbort();

            behaviour?.OnStarted(this);
        }

        private async void WaitingForAbort()
        {
            try
            {
                if (writeTask != null)
                {
                    await writeTask;
                }

                if (readTask != null)
                {
                    await readTask;
                }
            }
            catch (Exception ex)
            {
                ProcessError(ex);
            }
            finally
            {
                if (state == ClientState.Running)
                {
                    await ProcessStopAsync();
                }
            }
        }

        protected virtual async Task WriteAsync(PipeWriter writer)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task ReadAsync(PipeReader reader)
        {
            await Task.CompletedTask;
        }

        protected abstract ValueTask StartPipe(out PipeWriter writer, out PipeReader reader);
        protected abstract ValueTask StopPipe();
    }
}