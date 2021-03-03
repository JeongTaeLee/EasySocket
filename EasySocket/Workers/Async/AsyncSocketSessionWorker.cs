using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Workers.Async
{
    public class AsyncSocketSessionWorker : BaseSocketSessionWorker
    {
        public AsyncSocketSessionWorker(ISocketServerWorker server)
            : base(server)
        {
        }
        
           
    }
}