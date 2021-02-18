using System;
using System.Threading.Tasks;

namespace EasySocket.Channels
{
    public interface ISessionChannel
    {
        void Send(byte[] buffer, int offset, int count);
        void Send(ArraySegment<byte> segment);

        Task SendAsync(byte[] buffer, int offset, int count);
        Task SendAsync(ArraySegment<byte> segment);
    }
}
