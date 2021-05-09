using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.Factories;

namespace EasySocket.Server
{
    public enum ServerState
    {
        None = 0,
        Starting,
        Running,
        Stopping,
        Stopped,
    }

    public interface IServer<TServer> : IServer
        where TServer : IServer<TServer>
    {
        TServer SetLoggerFactory(ILoggerFactory lgerFctry);
        TServer SetMsgFilterFactory(IMsgFilterFactory msgFltrFctry);
        TServer SetSessionConfigrator(Action<ISession> ssnCnfgtr);
        TServer SetOnError(Action<TServer, Exception> onErr);
    }

    public interface IServer
    {
        ServerState state { get; }

        ISession[] sessions {get; }
        int sessionCount { get; }

        ValueTask StopAsync();
        
        ISession GetSessionById(string ssn);
    }
}