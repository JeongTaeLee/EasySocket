using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketSessionWorker : BaseSocketSessionWorker
    {
        public override void Send(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SendAsync(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}