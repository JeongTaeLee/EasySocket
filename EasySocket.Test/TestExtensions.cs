using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;

namespace EasySocket.Test
{
    public static class TestExtensions
    {
        // public static TServer CreateSocketServer<TServer, TSession>(ListenerConfig config, StringServerBehavior srvBhvr = null, StringSessionBehavior ssnBhvr = null)
        //     where TServer : SocketServer<TServer, TSession, string>, new()
        //     where TSession : SocketSession<TSession, string>
        // {
        //     return new TServer()
        //             .AddListener(config)
        //             .SetLoggerFactory(new ConsoleLoggerFactory())
        //             .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter, string>())
        //             .SetServerBehavior(srvBhvr ?? new StringServerBehavior())
        //             .SetSessionConfigrator((ssn) =>
        //             {
        //                 if (ssnBhvr != null)
        //                 {
        //                     ssn.SetSessionBehavior(ssnBhvr);
        //                 }
        //             });
        // }

        public static TcpSocketServer<string> CreateStringTcpServer(ListenerConfig listenerConfig, StringServerBehavior serverBehavior= null, StringSessionBehavior sessionBehavior = null)
        {
            return new TcpSocketServer<string>()
                .AddListener(listenerConfig)
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new StringMsgFilterFactory())
                .SetServerBehavior(serverBehavior ?? new StringServerBehavior())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehavior(sessionBehavior ?? new StringSessionBehavior());
                });

        }
    }
}