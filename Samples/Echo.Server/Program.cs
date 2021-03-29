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
    internal class EchoServerBehavior : IServerBehavior<string>
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnSessionConnected(IServer<string> server, ISession<string> ssn)
        {
            _logger.Info($"Connected Session : {ssn}");
        }

        public void OnSessionDisconnected(IServer<string> server, ISession<string> ssn)
        {
            _logger.Info($"Disconnected Session : {ssn}");
        }

        public void OnError(IServer<string> server, Exception ex)
        {
            _logger.Error(ex);
        }
    }
    internal class EchoSessionBehavior : ISessionBehavior<string>
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void OnStarted(ISession<string> ssn)
        {
            _logger.Info($"Started Session : {this}");
        }

        public void OnStopped(ISession<string> ssn)
        {
            _logger.Info($"Closed  Session : {this}");
        }

        public async void OnReceived(ISession<string> ssn, string packet)
        {
            var buffer = new byte[1048576];

            _logger.Info(packet);

            if (packet == "Bye")
            {
                await ssn.StopAsync();
                return;
            }

            var sendByte = Encoding.Default.GetBytes(packet);

            await ssn.SendAsync(sendByte);
        }

        public void OnError(ISession<string> ssn, Exception ex)
        {
            _logger.Error(ex);
        }
    }

    internal class EchoFilter : IMsgFilter<string>
    {
        public string Filter(ref SequenceReader<byte> sequence)
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
        private static async Task Main(string[] args)
        {
            var loggerFactory = new Echo.Server.Logging.NLogLoggerFactory("NLog.config");

            var server = new TcpSocketServer<string>()
                .AddListener(new ListenerConfig("127.0.0.1", 9199, 1000))
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<EchoFilter, string>())
                .SetLoggerFactory(loggerFactory)
                .SetSessionConfigrator(ssn =>
                {
                    ssn.SetSessionBehavior(new EchoSessionBehavior());
                })
                .SetServerBehavior(new EchoServerBehavior());

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
