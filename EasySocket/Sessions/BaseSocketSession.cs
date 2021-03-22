using System;
using System.Buffers;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Logging;
using EasySocket.Servers;
using EasySocket.Behaviors;
using EasySocket.SocketProxys;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Sessions
{
    public delegate void BaseSocketSessionCloseHandler(BaseSocketSession session);
    
    public abstract class BaseSocketSession : ISocketSession
    {
#region ISocketSession Field
        public ISocketServer server { get; private set; } = null;
        public ISessionBehavior behavior { get; private set; } = null;

        public ISocketSession.State state => (ISocketSession.State)_state;
#endregion

        private ISocketProxy _socketProxy = null;
        private IMsgFilter _msgFilter = null;
        private BaseSocketSessionCloseHandler _onClose = null;

        private int _state = (int)ISocketSession.State.None;

        protected ILogger logger { get; private set; } = null;
        
#region ISocketSession Method
        public void Stop()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ISocketSession.State.Closing, (int)ISocketSession.State.Running);
            if (prevState != (int)ISocketSession.State.Running)
            {
                if (prevState == (int)ISocketSession.State.None)
                {
                    _onClose?.Invoke(this);
                }

                return;
            }

            var socketProxy = _socketProxy;
            if (socketProxy == null)
            {
                return;
            }
            
            if (Interlocked.CompareExchange(ref this._socketProxy, null, socketProxy) == socketProxy)
            {
                socketProxy.Stop();
                _state = (int)ISocketSession.State.Closed;
            }
        }

        public virtual async Task StopAsync()
        {
            int prevState = Interlocked.CompareExchange(ref _state, (int)ISocketSession.State.Closing, (int)ISocketSession.State.Running);
            if (prevState != (int)ISocketSession.State.Running)
            {
                if (prevState == (int)ISocketSession.State.None)
                {
                    _onClose?.Invoke(this);
                }

                return;
            }

            var socketProxy = _socketProxy;
            if (socketProxy == null)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref this._socketProxy, null, socketProxy) == socketProxy)
            {
                await socketProxy.StopAsync();
                _state = (int)ISocketSession.State.Closed;
            }
        }

        public int Send(ReadOnlyMemory<byte> sendMemory)
        {
            if (_state != (int)ISocketSession.State.Running)
            {
                return -1;
            }

            return _socketProxy.Send(sendMemory);
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            if (_state != (int)ISocketSession.State.Running)
            {
                return -1;
            }

            return await _socketProxy.SendAsync(sendMemory);
        }

        public ISocketSession SetSessionBehavior(ISessionBehavior behavior)
        {
            this.behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            
            return this;
        }
        #endregion ISocketSession Method

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
                         "\"ISocketSession.loggerFactory\" returned null : Unable to get Logger.");

            _msgFilter = server.msgFilterFactory.Get() ??
                         throw new InvalidOperationException(
                             "\"ISocketSession.msgFilterFactory\" returned null : Unable to get MsgFilter.");

            _socketProxy = CreateSocketProxy() ??
                           throw new InvalidOperationException("\"CreateSocketProxy\" Method returned null.");

            int prevState = Interlocked.CompareExchange(ref _state, (int)ISocketSession.State.Running, (int)ISocketSession.State.None);
            if (prevState != (int)ISocketSession.State.None)
            {
                throw new InvalidOperationException("ISocketSession state before startup is invalid.");
            }

            _socketProxy.onReceived = OnReceivedFromSocketProxy;
            _socketProxy.onError = OnErrorFromSocketProxy;
            _socketProxy.onClose = OnCloseFromSocketProxy;
            _socketProxy.Start(sck, server.service.loggerFactroy.GetLogger(_socketProxy.GetType()));

            behavior?.OnStarted(this);
        }

        private void OnCloseFromSocketProxy()
        {
            behavior?.OnClosed(this);
            _onClose.Invoke(this);
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
        /// <see cref="ISocketSession"/>를 소유하는 <see cref="ISocketServer"/> 입니다.
        /// </summary>
        public BaseSocketSession SetSocketServer(ISocketServer _srv)
        {
            server = _srv ?? throw new ArgumentNullException(nameof(_srv));
            return this;
        }
        
        /// <summary>
        /// <see cref="ISocketSession"/>가 종료될 때 호출하는 콜백 함수인 <see cref="BaseSocketSessionCloseHandler"/>를 등록합니다.
        /// </summary>
        public BaseSocketSession SetCloseHandler(BaseSocketSessionCloseHandler onClose)
        {
            _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
            return this;
        }

        /// <summary>
        /// <see cref="ISocketSession"/>의 소켓 통신이 구현된 <see cref="ISocketProxy"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract ISocketProxy CreateSocketProxy();

    }
}