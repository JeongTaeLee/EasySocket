using System;
using System.Threading.Tasks;
using EasySocket.Servers;
using EasySocket.Behaviors;
namespace EasySocket.Sessions
{
    public interface ISocketSession
    {
        public enum State
        {
            None = 0, // 시작 전.
            Running, // 작동중
            Closing, // 종료 처리 중
            Closed,
        }

        /// <summary>
        /// <see cref="ISocketSession"/>을 소유하는 <see cref="ISocketServer"/> 입니다.
        /// </summary>
        ISocketServer server { get; }

        /// <summary>
        /// <see cref="ISocketSession"/>의 이벤트를 처리하는 <see cref="ISessionBehavior"/> 입니다. 
        /// </summary>
        ISessionBehavior behavior { get; }

        /// <summary>
        /// <see cref="ISocketSession"/>의 상태를 나타내는 Flag 입니다.
        /// </summary>
        State state { get; }

        /// <summary>
        /// <see cref="ISocketSession"/>의 이벤트를 처리하는 <see cref="ISessionBehavior"/>를 설정합니다. 
        /// </summary>
        ISocketSession SetSessionBehavior(ISessionBehavior behavior);

        /// <summary>
        /// 동기 방식으로 <see cref="ISocketSession"/>를 중지합니다.
        /// </summary>
        void Close();

        /// <summary>
        /// 비동기 방식으로 <see cref="ISocketSession"/>를 중지합니다.
        /// </summary>
        Task StopAsync();

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