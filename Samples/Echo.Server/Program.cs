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
    internal class EchoSessionBehavior : ISessionBehaviour
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

        public void OnStopped(ISession ssn)
        {
            _logger.Info($"Stopped Session(Before) : {this}");
        }

        public ValueTask OnReceived(ISession ssn, object packet)
        {
            var strPacket = packet.ToString();

            if (strPacket == "Ping")
            {
                var pongPacket = Encoding.Default.GetBytes("Pong");
                var sendPacket = new byte[pongPacket.Length + 4];
                    
                Buffer.BlockCopy(BitConverter.GetBytes(4), 0, sendPacket, 0, 4);
                Buffer.BlockCopy(pongPacket, 0, sendPacket, 4, pongPacket.Length);

                ssn.SendAsync(sendPacket);
                _logger.Info("Sended Ping");
            }

            return new ValueTask();
        }

        public void OnError(ISession ssn, Exception ex)
        {
            _logger.Error(ex);
        }

    }

    internal class EchoFilter : FixedHeaderMsgFilter
    {
        public EchoFilter()
            : base(4)
        {

        }

        protected override int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            return BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
        }

        protected override object ParseMsgInfo(ref ReadOnlySequence<byte> headerSeq, ref ReadOnlySequence<byte> bodySeq)
        {
            return Encoding.Default.GetString(bodySeq);
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
                    ssn.SetSessionBehaviour(new EchoSessionBehavior());
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
