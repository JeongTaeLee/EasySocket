using System;
using System.Threading.Tasks;
using EasySocket.Behaviors;

namespace EasySocket.Workers
{
    public interface ISocketSessionWorker
    {
        /// <summary>
        /// 해당 세션을 소유하는 <see cref="ISocketServerWorker"/> 입니다.
        /// </summary>
        public ISocketServerWorker server { get; }

        /// <summary>
        /// 해당 <see cref="ISocketSessionWorker"/>의 이벤트를 처리하는 <see cref="ISessionBehavior"/> 입니다. 
        /// </summary>
        public ISessionBehavior behavior { get; }

        /// <summary>
        /// 해당 <see cref="ISocketSessionWorker"/>의 이벤트를 처리하는 <see cref="ISessionBehavior"/>를 설정합니다. 
        /// </summary>
        public ISocketSessionWorker SetSessionBehavior(ISessionBehavior behavior);

    }
}