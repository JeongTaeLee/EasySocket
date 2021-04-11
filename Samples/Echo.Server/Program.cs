using System;
using System.Text;
using System.Buffers;
using System.Threading.Tasks;
using NLog;
using EasySocket.Common.Protocols.MsgFilters;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;

namespace Echo.Server
{
    internal class EchoSessionBehavior : ISessionBehavior
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnStartBefore(ISession ssn)
        {
            _logger.Info($"Start Session(Before) : {this}");
        }

        public void OnStartAfter(ISession ssn)
        {
            _logger.Info($"Start Session(After) : Count({Program.server.sessionCount})");
        }

        public void OnStopBefore(ISession ssn)
        {
            _logger.Info($"Stop Session(Before) : {this}");
        }

        public void OnStopAfter(ISession ssn)
        {
            _logger.Info($"Stop Session(After) : Count({Program.server.sessionCount})");
        }
        public void OnReceived(ISession ssn, object packet)
        {
            var strPacket = packet.ToString();

            if (strPacket == "Ping")
            {
                ssn.SendAsync(Encoding.Default.GetBytes("Pong"));
            }
        }

        public void OnError(ISession ssn, Exception ex)
        {
            _logger.Error(ex);
        }

    }

    internal class EchoFilter : IMsgFilter
    {
        public object Filter(ref SequenceReader<byte> sequence)
        {
            var buffer = sequence.Sequence.Slice(0, sequence.Length);
            sequence.Advance(sequence.Length);
            
            return Encoding.Default.GetString(buffer);
        }

        public void Reset()
        {
        }
    }

    internal static class Program
    {
        static ILogger logger = LogManager.GetCurrentClassLogger();
        public static TcpSocketServer server = null;

        private static async Task Main(string[] args)
        {
            var loggerFactory = new Echo.Server.Logging.NLogLoggerFactory("NLog.config");

            server = new TcpSocketServer()
                .AddListener(new ListenerConfig("127.0.0.1", 9199, 1000))
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<EchoFilter>())
                .SetLoggerFactory(loggerFactory)
                .SetOnError((server, ex) =>
                {
                    logger.Error(ex);
                })
                .SetSessionConfigrator(ssn =>
                {
                    ssn.SetSessionBehavior(new EchoSessionBehavior());
                });

            await server.StartAsync();

            while (true)
            {
                var inputStr = Console.ReadLine();
                if (inputStr == "close")
                {
                    break;
                }
            }

            await server.StopAsync();
        }
    }
}
