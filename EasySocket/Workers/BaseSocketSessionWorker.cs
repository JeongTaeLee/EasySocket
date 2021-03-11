using System;
using System.Buffers;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Behaviors;
using EasySocket.SocketProxys;
using EasySocket.Logging;
using EasySocket.Protocols.Filters;

namespace EasySocket.Workers
{
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
#region ISocketSessionWorker Field
        public ISocketServerWorker server { get; private set; } = null;
        public ISessionBehavior behavior { get; private set; } = null;
#endregion

        private ISocketProxy _socketProxy = null;
        private IMsgFilter _msgFilter = null;

        protected ILogger logger { get; private set; } = null;

#region ISocketSessionWorker Method
        public void Close()
        {
            var socketProxy = _socketProxy;
            if (socketProxy == null)
            {
                return;
            }
            
            if (Interlocked.CompareExchange(ref this._socketProxy, null, socketProxy) == socketProxy)
            {
                socketProxy.Close();
            }
        }

        public virtual ValueTask CloseAsync()
        {
            var socketProxy = _socketProxy;
            if (socketProxy == null)
            {
                return ValueTask.CompletedTask;
            }

            if (Interlocked.CompareExchange(ref this._socketProxy, null, socketProxy) == socketProxy)
            {
                return _socketProxy.CloseAsync();
            }

            return ValueTask.CompletedTask;
        }

        public int Send(ReadOnlyMemory<byte> sendMemory)
        {
            return _socketProxy.Send(sendMemory);
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            return _socketProxy.SendAsync(sendMemory);
        }

        public ISocketSessionWorker SetSessionBehavior(ISessionBehavior behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            this.behavior = behavior;

            return this;
        }
#endregion ISocketSessionWorker Method

        public void Start(ISocketServerWorker server, Socket socket)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
        
            this.server = server;
            this._msgFilter = server.msgFilterFactory.Get();
            this.logger = server.service.loggerFactroy.GetLogger(GetType());

            _socketProxy = CreateSocketProxy();
            if (_socketProxy == null)
            {
                throw new InvalidOperationException("\"CreateSocketProxy\" Method returned null.");
            }

            _socketProxy.received = OnReceivedFromSocketProxy;
            _socketProxy.error = OnErrorFromSocketProxy;

            _socketProxy.Start(socket, server.service.loggerFactroy.GetLogger(_socketProxy.GetType()));
            
            if (behavior != null)
            {
                behavior.OnStarted();
            }
        }
        
        private long OnReceivedFromSocketProxy(ref ReadOnlySequence<byte> sequence)
        {
            try
            {
                var sequenceReader = new SequenceReader<byte>(sequence);

                while (sequence.Length > sequenceReader.Consumed)
                {
                    var msgInfo = _msgFilter.Filter(ref sequenceReader);

                    if (msgInfo == null)
                    {
                        break;
                    }

                    behavior?.OnReceived(msgInfo);
                }

                return (int)sequenceReader.Consumed;
            }
            catch (Exception ex)
            {
                behavior?.OnError(ex);

                return (int)sequence.Length;
            }
        }

        private void OnErrorFromSocketProxy(Exception ex)
        {
            behavior?.OnError(ex);
        }

        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 소켓 통신이 구현된 <see cref="ISocketProxy"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract ISocketProxy CreateSocketProxy();
    }
}