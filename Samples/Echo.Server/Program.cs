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
    internal class EchoServerBehavior : IServerBehavior
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnSessionConnected(ISocketSessionWorker session)
        {
            _logger.Info($"Connected Session : {session}");
        }

        public void OnSessionDisconnected(ISocketSessionWorker session)
        {
            _logger.Info($"Disconnected Session : {session}");
        }
        
        public void OnError(Exception ex)
        {
            _logger.Error(ex);
        }
    };

    internal class EchoSessionBehavior : ISessionBehavior
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnStarted(ISocketSessionWorker session)
        {
            _logger.Info($"Started Session : {this}");
        }

        public void OnClosed(ISocketSessionWorker session)
        {
            _logger.Info($"Closed  Session : {this}");
        }

        public void OnError(ISocketSessionWorker session, Exception ex)
        {
            _logger.Error(ex);
        }

        public async void OnReceived(ISocketSessionWorker session, IMsgInfo msg)
        {
            var convertedMsg = msg as EchoMsgInfo;
            if (convertedMsg == null)
            {
                return;
            }

            _logger.Info(convertedMsg.str);

            var sendByte = Encoding.Default.GetBytes("Close");

            await session.SendAsync(sendByte);

            await session.CloseAsync();
        }
    }

    internal class EchoMsgInfo : IMsgInfo
    {
        public string str { get; private set; }

        public EchoMsgInfo(string str)
        {
            this.str = str;
        }

    }

    internal class EchoFilter : IMsgFilter
    {
        public IMsgInfo Filter(ref SequenceReader<byte> sequence)
        {
            var buffer = sequence.Sequence.Slice(0, sequence.Length);
            sequence.Advance(sequence.Length);
            
            return new EchoMsgInfo(Encoding.Default.GetString(buffer));
        }

        public void Reset()
        {
        }
    }

    internal static class Program
    {
        private static void Main(string[] args)
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
                var inputStr = Console.ReadLine();
                if (inputStr == "close")
                {
                    break;
                }
            }
        }
    }
}
