using System;
using System.Buffers;
using System.Text;
using EasySocket.Common.Protocols.MsgInfos;
using EasySocket.Common.Protocols.MsgFilters;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using NLog;
using EasySocket.Server;
using System.Threading.Tasks;
using EasySocket.Server.Listeners;

namespace Echo.Server
{
    internal class EchoServerBehavior : IServerBehavior
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnSessionConnected(IServer server, ISession ssn)
        {
            _logger.Info($"Connected Session : {ssn}");
        }

        public void OnSessionDisconnected(IServer server, ISession ssn)
        {
            _logger.Info($"Disconnected Session : {ssn}");
        }

        public void OnError(IServer server, Exception ex)
        {
            _logger.Error(ex);
        }
    }
    internal class EchoSessionBehavior : ISessionBehavior
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnStarted(ISession ssn)
        {
            _logger.Info($"Started Session : {this}");
        }

        public void OnStopped(ISession ssn)
        {
            _logger.Info($"Closed  Session : {this}");
        }

        public async void OnReceived(ISession ssn, IMsgInfo msgInfo)
        {
            var buffer = new byte[1048576];

            var convertedMsg = msgInfo as EchoMsgInfo;
            if (convertedMsg == null)
            {
                return;
            }

            _logger.Info(convertedMsg.str);

            if (convertedMsg.str == "Bye")
            {
                await ssn.StopAsync();
                return;
            }

            var sendByte = Encoding.Default.GetBytes(convertedMsg.str);

            await ssn.SendAsync(sendByte);
        }

        public void OnError(ISession ssn, Exception ex)
        {
            _logger.Error(ex);
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
        private static async Task Main(string[] args)
        {
            var loggerFactory = new Echo.Server.Logging.NLogLoggerFactory("NLog.config");

            var server = new TcpSocketServer()
                .AddListener(new ListenerConfig("127.0.0.1", 9199, 1000))
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<EchoFilter>())
                .SetServerBehavior(new EchoServerBehavior())
                .SetSessionConfigrator(ssn =>
                {
                    ssn.SetSessionBehavior(new EchoSessionBehavior());
                })
                .SetLoggerFactroy(loggerFactory);

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
