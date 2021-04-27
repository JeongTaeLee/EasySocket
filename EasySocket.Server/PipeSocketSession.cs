using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace EasySocket.Server
{
    public abstract class PipeSocketSession<TSession> : SocketSession<TSession>
        where TSession : PipeSocketSession<TSession>
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

        protected override async ValueTask OnStartedAsync()
        {
            if (state != SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid state. : Session state is {state}");
            }

            WaitingForAbort();

            if (behaviour != null)
            {
                await behaviour.OnStartAfterAsync(this);
            }
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
                if (state == SessionState.Running)
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