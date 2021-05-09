using System;
using System.Threading.Tasks;

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
        string id { get; }
        SessionState state { get; }     
        ISessionBehaviour behaviour { get; }

        ValueTask StopAsync();
        
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        ISession SetSessionBehaviour(ISessionBehaviour bhvr);
    }
}