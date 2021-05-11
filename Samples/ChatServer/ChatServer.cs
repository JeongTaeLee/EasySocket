using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;
using EasySocket.Server.Logging;
using NLog;
using System;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ChatServer
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly ChatManager _chatSessionManager;

        public ChatServer()
        {
            _chatSessionManager = new ChatManager(this);
        }

        public async ValueTask StartAsync(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 서버 시작.
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new NLogLoggerFactory("NLog.config"))
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<FixedHeaderJsonMsgFilter>())
                .SetSessionConfigrator(ConfigureSession)
                .SetOnError(OnServerError);

            await server.StartAsync(new ListenerConfig("127.0.0.1", 9199, 500));
            _logger.Info("Server is running on 127.0.0.1:9199");

            Console.ReadKey();
            await server.StopAsync();
        }

        private void ConfigureSession(ISession session)
        {
            session.SetSessionBehaviour(this);
        }

        private void OnServerError(TcpStreamPipeSocketServer server, Exception e)
        {
            _logger.Error(e, $"Server - OnError");
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error(e.ExceptionObject as Exception, $"AppDomain - OnUnhandledException");
        }
    }
}
