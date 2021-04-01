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

    public interface ISession<TPacket> : ISession
    {
        public ISessionBehavior<TPacket> behavior { get; }

        ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry);

        public ISession<TPacket> SetSessionBehavior(ISessionBehavior<TPacket> bhvr);
    } 

    public interface ISession
    {
        public string sessionId { get; 
        }
        SessionState state { get; }     

        ValueTask StopAsync();
    }
}