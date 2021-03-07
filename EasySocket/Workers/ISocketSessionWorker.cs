using EasySocket.Behaviors;
using EasySocket.Logging;
using EasySocket.Protocols.Filters;

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

    }
}