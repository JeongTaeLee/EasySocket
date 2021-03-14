using System;
using System.Buffers;
using System.Text;
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
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

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
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

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
            var convertedMsg = msg as EchoMsgInfo;
            if (convertedMsg == null)
            {
                return;
            }

            _logger.Info(convertedMsg);
        }
    }

    class EchoMsgInfo : IMsgInfo
    {
        public string str { get; private set; } = string.Empty;

        public EchoMsgInfo(string str)
        {
            this.str = str;
        }

    }

    class EchoFilter : IMsgFilter
    {
        public EchoFilter()
        {

        }

        public IMsgInfo Filter(ref SequenceReader<byte> sequence)
        {
            var buffer = sequence.TryRead(out byte bt);

            return new EchoMsgInfo(Encoding.Default.GetString(sequence.Sequence.Slice(0, sequence.Length)));
        }

        public void Reset()
        {
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
