using System;
using System.Buffers;
using System.Text;
using EasySocket;
using EasySocket.Behaviors;
using EasySocket.Listeners;
using EasySocket.Protocols.Filters;
using EasySocket.Protocols.Filters.Factories;
using EasySocket.Protocols.MsgInfos;
using EasySocket.Servers;
using EasySocket.Servers.Async;
using EasySocket.Sessions;
using NLog;

namespace Echo.Server
{
    internal class EchoServerBehavior : IServerBehavior
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnSessionConnected(ISocketSession session)
        {
            _logger.Info($"Connected Session : {session}");
        }

        public void OnSessionDisconnected(ISocketSession session)
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

        public void OnStarted(ISocketSession session)
        {
            _logger.Info($"Started Session : {this}");
        }

        public void OnClosed(ISocketSession session)
        {
            _logger.Info($"Closed  Session : {this}");
        }

        public void OnError(ISocketSession session, Exception ex)
        {
            _logger.Error(ex);
        }

        public async void OnReceived(ISocketSession session, IMsgInfo msg)
        {
            var buffer = new byte[1048576];

            var convertedMsg = msg as EchoMsgInfo;
            if (convertedMsg == null)
            {
                return;
            }

            _logger.Info(convertedMsg.str);

            if (convertedMsg.str == "Bye")
            {
                await session.CloseAsync();
                return;
            }

            var sendByte = Encoding.Default.GetBytes(convertedMsg.str);
            
            await session.SendAsync(sendByte);
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
                .SetSocketServer<AsyncSocketServer>()
                .SetSocketServerConfigrator((server) =>
                {
                    server
                        .AddListener(new ListenerConfig("Any", 9199, 100000, true))
                        .SetMsgFilterFactory(new DefaultMsgFilterFactory<EchoFilter>())
                        .SetServerBehavior(new EchoServerBehavior())
                        .SetServerConfig(new SocketServerConfig
                        {

                        })
                        ;
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
