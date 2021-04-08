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

        public static TcpSocketServer CreateStringTcpServer(ListenerConfig listenerConfig, EventSessionBehavior sessionBehavior = null)
        {
            return new TcpSocketServer()
                .AddListener(listenerConfig)
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehavior(sessionBehavior ?? new EventSessionBehavior());
                });

        }
    }
}