using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;

namespace EasySocket.Server.Listeners
{
    public abstract class BaseListener : IListener
    {
        public ListenerState state => (ListenerState)_state;
        public ListenerConfig config { get; private set; } = null;
        public ListenerAcceptHandler onAccept { get; set; }
        public ListenerErrorHandler onError { get; set; }

        private int _state = (int)ListenerState.None;
        protected ILogger logger { get; set; } = null;

        public async Task StartAsync(ListenerConfig cnfg, ILogger lger)
        {
            if (state == ListenerState.Stopped)
            {
                throw ExceptionExtensions.TerminatedObjectIOE("Listener");
            }

            if (cnfg == null)
            {
                throw new ArgumentNullException(nameof(cnfg));
            }

            if (string.IsNullOrEmpty(cnfg.ip))
            {
                throw new ArgumentNullException(nameof(cnfg.ip));
            }

            if (0 > cnfg.port)
            {
                throw new ArgumentException("Invalid Port Range");
            }

            if (lger == null)
            {
                throw new ArgumentNullException(nameof(lger));
            }

            int prevState = Interlocked.CompareExchange(ref _state, (int)ListenerState.Starting, (int)ListenerState.None);
            if (prevState != (int)ListenerState.None)
            {
                throw ExceptionExtensions.CantStartObjectIOE("Listener", (ListenerState)prevState);
            }

            config = cnfg;
            logger = lger;

            try
            {
                if (onAccept == null)
                {
                    logger.MemberNotSetUsePropWarn(nameof(onAccept));
                }

                if (onError == null)
                {
                    logger.MemberNotSetUsePropWarn(nameof(onError));
                }

                await InternalStartAsync();

                _state = (int)ListenerState.Running;
            }
            finally
            {
                if (state != ListenerState.Running)
                {
                    _state = (int)ListenerState.None;
                }
            }
        }

        public async Task StopAsync()
        {
            if (state != ListenerState.Running)
            {
                throw ExceptionExtensions.CantStopObjectIOE("Listener", state);
            }

            await ProcessStop();
        }

        protected virtual async ValueTask ProcessStop()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ListenerState.Stopping, (int)ListenerState.Running);
            if (prevState != (int)ListenerState.Running)
            {
                throw ExceptionExtensions.CantStopObjectIOE("Listener", state);
            }

            await InternalStopAsync();

            _state = (int)ListenerState.Stopped;
        }

        protected async void ProcessAccept(Socket sck)
        {
            if (state != ListenerState.Running)
            {
                throw ExceptionExtensions.InvalidObjectStateIOE("Listener", state);
            }

            if (onAccept == null)
            {
                return;
            }

            await onAccept.Invoke(this, sck);
        }

        protected virtual void ProcessError(Exception ex)
        {
            onError?.Invoke(this, ex);
        }

        protected virtual ValueTask InternalStartAsync() { return new ValueTask(); }
        protected virtual ValueTask InternalStopAsync() { return new ValueTask(); }

    }
}