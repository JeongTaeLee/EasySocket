using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Server
{
    public enum SessionState
    {
        None = 0,
        Starting,
        Running,
        Stopping,
        Stopped,
    }

    public interface ISession
    {
        string sessionId { get; }
        SessionState state { get; }     
        ISessionBehavior behavior { get; }

        ValueTask StopAsync();
        
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        ISession SetSessionBehavior(ISessionBehavior bhvr);
    }
}