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
        public bool isClosed { get; private set; } = false;
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

                isClosed = true;
            }
        }

        public virtual async ValueTask CloseAsync()
        {
            var socketProxy = _socketProxy;
            if (socketProxy == null)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref this._socketProxy, null, socketProxy) == socketProxy)
            {
                await _socketProxy.CloseAsync();

                isClosed = true;
            }
        }

        public int Send(ReadOnlyMemory<byte> sendMemory)
        {
            if (_socketProxy == null)
            {
                return -1;
            }

            return _socketProxy.Send(sendMemory);
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            if (_socketProxy == null)
            {
                return -1;
            }

            return await _socketProxy.SendAsync(sendMemory);
        }

        public ISocketSessionWorker SetSessionBehavior(ISessionBehavior behavior)
        {
            this.behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            
            return this;
        }
#endregion ISocketSessionWorker Method

        public void Start(ISocketServerWorker srv, Socket sck)
        {
            if (sck == null)
            {
                throw new ArgumentNullException(nameof(sck));
            }

            server = srv ?? throw new ArgumentNullException(nameof(srv));
        
            logger = server.service.loggerFactroy.GetLogger(GetType());
            if (logger == null)
            {
                throw new InvalidOperationException("\"ISocketSessionWorker.loggerFactory\" returned null : Unable to get Logger.");
            }

            _msgFilter = server.msgFilterFactory.Get();
            if (_msgFilter == null)
            {
                throw new InvalidOperationException("\"ISocketSessionWorker.msgFilterFactory\" returned null : Unable to get MsgFilter.");
            }

            _socketProxy = CreateSocketProxy();
            if (_socketProxy == null)
            {
                throw new InvalidOperationException("\"CreateSocketProxy\" Method returned null.");
            }

            _socketProxy.onReceived = OnReceivedFromSocketProxy;
            _socketProxy.onError = OnErrorFromSocketProxy;
            _socketProxy.onClose = OnCloseFromSocketProxy;
            _socketProxy.Start(sck, server.service.loggerFactroy.GetLogger(_socketProxy.GetType()));
            
            behavior?.OnStarted(this);
        }
        
        private void OnCloseFromSocketProxy()
        {
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

                    behavior?.OnReceived(this, msgInfo);
                }

                return (int)sequenceReader.Consumed;
            }
            catch (Exception ex)
            {
                behavior?.OnError(this, ex);

                return (int)sequence.Length;
            }
        }

        private void OnErrorFromSocketProxy(Exception ex)
        {
            behavior?.OnError(this, ex);
        }

        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 소켓 통신이 구현된 <see cref="ISocketProxy"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract ISocketProxy CreateSocketProxy();
    }
}