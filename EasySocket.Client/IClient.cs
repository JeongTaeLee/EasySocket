using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Client
{
    public enum ClientState
    {
        None = 0,
        Starting,
        Running,
        Stopping,
        Stopped,
    }

    public interface IClient
    {

        ClientState state { get; }
        IClientBehavior behavior { get; }
        
        Task StopAsync();
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}