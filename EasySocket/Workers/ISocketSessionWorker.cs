using System;
using System.Threading.Tasks;

namespace EasySocket.Workers
{
    public interface ISocketSessionWorker
    {
        /// <summary>
        /// 해당 세션을 소유하는 <see cref="EasySocket.Workers.ISocketServerWorker"/> 입니다.
        /// </summary>
        public ISocketServerWorker server { get; }

        void Send(byte[] buffer, int offset, int count);
        ValueTask SendAsync(byte[] buffer, int offset, int count);
    }
}