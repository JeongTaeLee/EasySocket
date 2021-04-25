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

        ISession[] sessions {get; }
        int sessionCount { get; }

        ValueTask StopAsync();
        
        ISession GetSessionById(string ssn);
    }
}