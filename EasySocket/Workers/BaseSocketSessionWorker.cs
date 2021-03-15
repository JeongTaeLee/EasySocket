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
    public delegate void BaseSocketSessionCloseHandler(BaseSocketSessionWorker session);
    
    public abstract class BaseSocketSessionWorker : ISocketSessionWorker
    {
#region ISocketSessionWorker Field
        public ISocketServerWorker server { get; private set; } = null;
        public ISessionBehavior behavior { get; private set; } = null;

        private int _isClosed = 0;
        public bool isClosed => _isClosed == 1;
        #endregion

        private ISocketProxy _socketProxy = null;
        private IMsgFilter _msgFilter = null;

        private BaseSocketSessionCloseHandler _onClose = null;
        
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
                InternalClose();
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
                await socketProxy.CloseAsync();
                InternalClose();
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

        public void Start(Socket sck)
        {
            if (sck == null)
            {
                throw new ArgumentNullException(nameof(sck));
            }

            if (server == null)
            {
                throw new InvalidOperationException("Server not set: Please call the \"SetSocketServer\" Method and set it up.");
            }
                        
            if (_onClose == null)
            {
                throw new InvalidOperationException("CloseHandler not set: Please call the \"SetCloseHandler\" Method and set it up.");
            }

            logger = server.service.loggerFactroy.GetLogger(GetType()) ??
                     throw new InvalidOperationException(
                         "\"ISocketSessionWorker.loggerFactory\" returned null : Unable to get Logger.");

            _msgFilter = server.msgFilterFactory.Get() ??
                         throw new InvalidOperationException(
                             "\"ISocketSessionWorker.msgFilterFactory\" returned null : Unable to get MsgFilter.");

            _socketProxy = CreateSocketProxy() ??
                           throw new InvalidOperationException("\"CreateSocketProxy\" Method returned null.");

            _socketProxy.onReceived = OnReceivedFromSocketProxy;
            _socketProxy.onError = OnErrorFromSocketProxy;
            _socketProxy.onClose = OnCloseFromSocketProxy;
            _socketProxy.Start(sck, server.service.loggerFactroy.GetLogger(_socketProxy.GetType()));
            
            behavior?.OnStarted(this);
        }

        private void InternalClose()
        {
            if (Interlocked.CompareExchange(ref _isClosed, 1, 0) == 0)
            {
                behavior?.OnClosed(this);

                _onClose.Invoke(this);
            }
        }

        private void OnCloseFromSocketProxy()
        {
            InternalClose();
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
        
        /// <summary>
        /// <see cref="ISocketSessionWorker"/>를 소유하는 <see cref="ISocketServerWorker"/> 입니다.
        /// </summary>
        public BaseSocketSessionWorker SetSocketServer(ISocketServerWorker _srv)
        {
            server = _srv ?? throw new ArgumentNullException(nameof(_srv));
            return this;
        }
        
        /// <summary>
        /// <see cref="ISocketSessionWorker"/>가 종료될 때 호출하는 콜백 함수인 <see cref="BaseSocketSessionCloseHandler"/>를 등록합니다.
        /// </summary>
        public BaseSocketSessionWorker SetCloseHandler(BaseSocketSessionCloseHandler onClose)
        {
            _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
            return this;
        }
    }
}