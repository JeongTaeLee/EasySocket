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

        public void OnError(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void OnSessionConnected(ISocketSessionWorker session)
        {
            Console.WriteLine($"Connected Session : {session}");
        }

        public void OnSessionDisconnected(ISocketSessionWorker session)
        {
            Console.WriteLine($"Disconnected Session : {session}");
        }
    };

    internal class EchoSessionBehavior : ISessionBehavior
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnStarted(ISocketSessionWorker session)
        {
            Console.WriteLine($"Started Session : {this}");
        }

        public void OnClosed(ISocketSessionWorker session)
        {
            Console.WriteLine($"Closed  Session : {this}");
        }

        public void OnError(ISocketSessionWorker session, Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void OnReceived(ISocketSessionWorker session, IMsgInfo msg)
        {
            var convertedMsg = msg as EchoMsgInfo;
            if (convertedMsg == null)
            {
                return;
            }

            Console.WriteLine(convertedMsg.str);
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
