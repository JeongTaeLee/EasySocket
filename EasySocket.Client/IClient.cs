using System;
using System.Buffers;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public interface IClient<TClient> : IClient
        where TClient : class, IClient<TClient>
    {
        TClient SetClientBehavior(IClientBehavior bhvr);
    }

    public interface IClient
    {
        bool isClose { get; }
        IClientBehavior behavior { get; }
 
        void Stop();
        Task StopAsync();

        int Send(ReadOnlyMemory<byte> sendMemory);
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}