using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters.Factories;

namespace EasySocket.Server
{
    public interface IServer<TServer> : IServer
        where TServer : IServer
    {
        TServer SetMsgFilterFactory(IMsgFilterFactory msgFltrFctr);
        TServer SetServerBehavior(IServerBehavior bhvr);
        TServer SetSessionConfigrator(Action<ISession> sessionConfigrator);
        TServer SetLoggerFactroy(ILoggerFactory lgrFctr);
    }

    public interface IServer
    {
        public enum State
        {
            None = 0,
            Starting,
            Running,
            Stopping,
            Stopped,
        }

        State state { get; }
        IMsgFilterFactory msgFilterFactory { get; }
        IServerBehavior behavior { get; }
        ILoggerFactory loggerFactory { get; }
        Action<ISession> sessionConfigrator { get; }


        Task StartAsync();
        Task StopAsync();
    }
}