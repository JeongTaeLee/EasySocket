using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketSessionWorker : BaseSocketSessionWorker
    {
        public override void Send(ReadOnlyMemory<byte> sendMemory)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            throw new NotImplementedException();
        }
    }
}