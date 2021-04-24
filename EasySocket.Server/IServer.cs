using System;
using System.Collections.Generic;
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

    public interface IServer
    {
        ServerState state { get; }
        int sessionCount { get; }
        IReadOnlyList<ISession> sessions { get; }

        ValueTask StopAsync();
        
        ISession GetSessionById(string ssn);
        ISession[] GetAllSession();
    }
}