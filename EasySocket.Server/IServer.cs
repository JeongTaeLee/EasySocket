using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters.Factories;

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
    public interface IServer<TServer, TPacket> : IServer<TPacket>
        where TServer : IServer<TPacket>
    {
        TServer SetMsgFilterFactory(IMsgFilterFactory<TPacket> msgFltrFctr);
        TServer SetServerBehavior(IServerBehavior<TPacket> bhvr);
        TServer SetSessionConfigrator(Action<ISession<TPacket>> sessionConfigrator);
        TServer SetLoggerFactroy(ILoggerFactory lgrFctr);
    }

    public interface IServer<TPacket>
    {
        ServerState state { get; }
        IServerBehavior<TPacket> behavior { get; }

        Task StartAsync();
        Task StopAsync();
    }
}