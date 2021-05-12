using System;
using System.Threading.Tasks;

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
        IClientBehaviour behaviour { get; }

        Task StopAsync();
        int Send(byte[] buffer);
        int Send(ArraySegment<byte> segement);
        Task<int> SendAsync(byte[] buffer);
        Task<int> SendAsync(ArraySegment<byte> segement);
        
    }
}