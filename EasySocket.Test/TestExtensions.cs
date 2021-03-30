using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;

namespace EasySocket.Test
{
    public static class TestExtensions
    {
        public static TServer CreateSocketServer<TServer, TSession>(ListenerConfig config, IServerBehavior<string> srvBhvr = null, ISessionBehavior<string> ssnBhvr = null)
            where TServer : SocketServer<TServer, TSession, string>, new()
            where TSession : SocketSession<TSession, string>
        {
            return new TServer().AddListener(config)
                    .SetLoggerFactory(new ConsoleLoggerFactory())
                    .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter, string>())
                    .SetServerBehavior(srvBhvr ?? new EventServerBehavior<string>())
                    .SetSessionConfigrator((ssn) =>
                    {
                        if (ssnBhvr != null)
                        {
                            ssn.SetSessionBehavior(ssnBhvr);
                        }
                    });
        }
    }
}