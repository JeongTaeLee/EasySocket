using System;
using System.Threading.Tasks;
using EasySocket.Behaviors;

namespace EasySocket.Workers
{
    public interface ISocketSessionWorker
    {
        /// <summary>
        /// <see cref="ISocketSessionWorker"/>을 소유하는 <see cref="ISocketServerWorker"/> 입니다.
        /// </summary>
        ISocketServerWorker server { get; }

        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 이벤트를 처리하는 <see cref="ISessionBehavior"/> 입니다. 
        /// </summary>
        ISessionBehavior behavior { get; }

        /// <summary>
        /// <see cref="ISocketSessionWorker"/>의 이벤트를 처리하는 <see cref="ISessionBehavior"/>를 설정합니다. 
        /// </summary>
        ISocketSessionWorker SetSessionBehavior(ISessionBehavior behavior);

        /// <summary>
        /// 동기 방식으로 <see cref="ISocketSessionWorker"/>를 중지합니다.
        /// </summary>
        void Close();

        /// <summary>
        /// 비동기 방식으로 <see cref="ISocketSessionWorker"/>를 중지합니다.
        /// </summary>
        ValueTask CloseAsync();

        /// <summary>
        /// 동기 방식으로 데이터를 전송합니다.
        /// </summary>
        int Send(ReadOnlyMemory<byte> sendMemory);

        /// <summary>
        /// 비동기 방식으로 데이터를 전송합니다.
        /// </summary>
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory);
    }
}