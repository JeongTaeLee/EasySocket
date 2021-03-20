using System;
using System.Collections.Generic;
using EasySocket.Logging;
using System.Net.Sockets;
using EasySocket.Sessions;
using EasySocket.Behaviors;
using EasySocket.Listeners;
using EasySocket.Protocols.Filters.Factories;

namespace EasySocket.Servers
{
    public abstract class BaseSocketServer<TSession> : ISocketServer
        where TSession : BaseSocketSession
    {
#region ISocketServer Field 
        public ISocketServerConfig config { get; private set; } = new SocketServerConfig();
        public EasySocketService service { get; private set; } = null;
        public IMsgFilterFactory msgFilterFactory { get; private set; } = null;
        public IReadOnlyList<ListenerConfig> listenerConfigs => _listenerConfigs;
        public IServerBehavior behavior { get; private set; } = null;
#endregion ISocketServer Field

        private List<ListenerConfig> _listenerConfigs = new List<ListenerConfig>();
        private IReadOnlyList<IListener> _listeners = null;
        
        protected ILogger logger { get; private set; } = null;

#region ISocketServer Method

        public void Start(EasySocketService srvc)
        {
            if (srvc == null)
            {
                throw new ArgumentNullException(nameof(srvc));
            }

            if (0 >= _listenerConfigs.Count)
            {
                throw new InvalidOperationException("At least one ListenerConfig is not set : Please call the \"AddListener\" Method and set it up.");
            }

            if (msgFilterFactory == null)
            {
                throw new InvalidOperationException("MsgFilterFactroy not set: Please call the \"SetMsgFilterFactory\" Method and set it up.");
            }
   
            service = srvc;
            logger = srvc.loggerFactroy.GetLogger(GetType());

            if (behavior == null)
            {
                logger.Warn("Server Behavior is not set. : Unable to receive events for the server. Please call the \"SetServerBehavior\" Method and set it up.");
            }

            StartListeners();
        }

        public ISocketServer AddListener(ListenerConfig lstnrCnfg)
        {
            _listenerConfigs.Add(lstnrCnfg);
            return this;
        }

        public virtual ISocketServer SetServerBehavior(IServerBehavior bhvr)
        {
            if (bhvr == null)
            {
                throw new ArgumentNullException(nameof(bhvr));
            }

            behavior = bhvr;

            return this;
        }

        public virtual ISocketServer SetServerConfig(ISocketServerConfig cnfg)
        {
            if (cnfg == null)
            {
                throw new ArgumentNullException(nameof(cnfg));
            }

            this.config = cnfg;

            return this;
        }

        public virtual ISocketServer SetMsgFilterFactory(IMsgFilterFactory msgFltrFctry)
        {
            if (msgFltrFctry == null)
            {
                throw new ArgumentNullException(nameof(msgFltrFctry));
            }

            msgFilterFactory = msgFltrFctry;

            return this;
        }
#endregion ISocketServer Method

        private void StartListeners()
        {
            var tempListeners = new List<IListener>();

            for (int index = 0; index < _listenerConfigs.Count; ++index)
            {
                var listenerConfig = listenerConfigs[index];
                if (listenerConfig == null)
                {
                    throw new Exception($"ListenerConfig is null : index({index})");
                }

                var listener = CreateListener();
                if (listener == null)
                {
                    throw new Exception($"Listener is null : index({index})");
                }

                listener.accepted = OnSocketAcceptedFromListeners;
                listener.error = OnErrorOccurredFromListeners;
                listener.Start(listenerConfig, service.loggerFactroy.GetLogger(logger.GetType()));

                tempListeners.Add(listener);

                logger.DebugFormat("Started listener : {0}", listenerConfig.ToString());
            }

            _listeners = tempListeners;
        }

        protected void OnSocketAcceptedFromListeners(IListener lstnr, Socket acptdSck)
        {
            TSession session = null;

            try
            {
                acptdSck.LingerState = new LingerOption(true, 0);

                acptdSck.SendBufferSize = config.sendBufferSize;
                acptdSck.ReceiveBufferSize = config.recvBufferSize;

                if (0 < config.sendTimeout)
                {
                    acptdSck.SendTimeout = config.sendTimeout;
                }

                if (0 < config.recvTimeout)
                {
                    acptdSck.ReceiveTimeout = config.recvTimeout;
                }

                acptdSck.NoDelay = config.noDelay;

                var msgFilter = msgFilterFactory.Get();
                if (msgFilter == null)
                {
                    return;
                }

                var tempSession = CreateSession();
                if (tempSession == null)
                {
                    return;
                }

                service.sessionConfigrator.Invoke(tempSession
                    .SetSocketServer(this)
                    .SetCloseHandler(OnCloseFromSocketSession));

                behavior?.OnSessionConnected(tempSession);

                // 시작하기전 상태를 체크합니다 None 상태가 아니라면 비정상적인 상황입니다.
                if (tempSession.state != ISocketSession.State.None)
                {
                    return;
                }

                tempSession.Start(acptdSck);
                
                // finally에서 오류 체크를 하기 위해 모든 작업이 성공적으로 끝난 후 대입해줍니다.
                session = tempSession;
            }
            catch (Exception ex)
            {
                behavior?.OnError(ex);
            }
            finally
            {
                // 세션을 생성하지 못하면 연결이 실패한 것으로 관리합니다.
                if (session == null)
                {
                    acptdSck?.SafeClose();
                }
            }
        }

        protected virtual void OnErrorOccurredFromListeners(IListener lstnr, Exception ex)
        {
            behavior?.OnError(ex);
        }

        protected virtual void OnCloseFromSocketSession(BaseSocketSession session)
        {
            behavior?.OnSessionDisconnected(session);
        }
        
        /// <summary>
        /// <see cref="ISocketSession"/>의 소켓 수락을 구현하는 <see cref="IListener"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract IListener CreateListener();

        /// <summary>
        /// <see cref="ISocketSession"/>의 연결된 소켓을 관리하는 <see cref="ISocketSession"/>를 생성 후 반환합니다.
        /// </summary>
        protected abstract TSession CreateSession();
    }
}