using System;
using System.Buffers;
using EasySocket;
using EasySocket.Behaviors;
using EasySocket.Listeners;
using EasySocket.Protocols.Filters;
using EasySocket.Protocols.Filters.Factories;
using EasySocket.Protocols.MsgInfos;
using EasySocket.Workers;
using EasySocket.Workers.Async;
using NLog;

namespace Echo.Server
{
    class EchoServerBehavior : IServerBehavior
    {
        private ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnError(Exception ex)
        {
            _logger.Error(ex);
        }

        public void OnSessionConnected(ISocketSessionWorker session)
        {
            _logger.Info($"Connected Session : {session}");
        }

        public void OnSessionDisconnected(ISocketSessionWorker session)
        {
            _logger.Info($"Disconnected Session : {session}");
        }
    };

    class EchoSessionBehavior : ISessionBehavior
    {
        private ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnStarted()
        {
            _logger.Info($"Started Session : {this}");
        }

        public void OnClosed()
        {
            _logger.Info($"Closed  Session : {this}");
        }

        public void OnError(Exception ex)
        {
            _logger.Error(ex);
        }

        public void OnReceived(IMsgInfo msg)
        {
           
        }
    }

    class EchoFilter : FixedHeaderMsgFilter
    {
        public EchoFilter()
            : base(4)
        {

        }

        protected override int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            return 0;
        }

        protected override IMsgInfo ParseMsgInfo(ref ReadOnlySequence<byte> headerSeq, ref ReadOnlySequence<byte> bodySeq)
        {
            return null;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var loggerFactory = new EasySocket.Logging.NLogLoggerFactory("NLog.config");

            var service = new EasySocketService()
                .SetLoggerFactroy(loggerFactory)
                .SetSocketServer<AsyncSocketServerWorker>()
                .SetSocketServerConfigrator((server) =>
                {
                    server
                        .AddListener(new ListenerConfig("Any", 9199, 100000, true))
                        .SetMsgFilterFactory(new DefaultMsgFilterFactory<EchoFilter>())
                        .SetServerBehavior(new EchoServerBehavior())
                        .SetServerConfig(new SocketServerWorkerConfig())
                        ;       
                })
                .SetSocketSessionConfigrator((session) =>
                {
                    session
                        .SetSessionBehavior(new EchoSessionBehavior())
                        ;
                });

            service.Start();

            while (true)
            {

            }
        }
    }
}
