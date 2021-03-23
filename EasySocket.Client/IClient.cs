using System;
using System.Buffers;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public delegate void ClientErrorHandler(IClient client, Exception ex);
    public delegate int ClientReceiveHandler(IClient client, ref ReadOnlySequence<byte> sequence);

    public interface IClient
    {
        ClientErrorHandler onError { get; set; }
        ClientReceiveHandler onReceived { get; set; }

        bool isClose { get; }

        void Stop();
        Task StopAsync();

        int Send(ReadOnlyMemory<byte> sendMemory);
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}