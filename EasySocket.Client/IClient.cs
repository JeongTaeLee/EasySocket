using System;
using System.Threading.Tasks;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.MsgFilters;

namespace EasySocket.Client
{
    public interface IClient
    {
        public enum State
        {
            None = 0,
            Starting,
            Running,
            Stopping,
            Stopped,
        }

        State state { get; }
        IMsgFilter msgFilter { get; }
        IClientBehavior behavior { get; }
        ILoggerFactory loggerFactroy { get; }

        void Start();
        Task StartAsync();

        void Stop();
        Task StopAsync();

        int Send(ReadOnlyMemory<byte> sendMemory);
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}