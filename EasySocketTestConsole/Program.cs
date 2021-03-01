using System;
using EasySocket;
using EasySocket.Workers;
using EasySocket.Workers.Async;
using EasySocket.Listeners;
using EasySocket.Behaviors;
using System.Net.Sockets;

namespace EasySocketTestConsole
{
    class TestBehavior : IServerBehavior
    {
        public void OnError(Exception ex)
        {
            throw new NotImplementedException();
        }

        public void OnSessionConnected()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <see cref="">
        /// </summary>
        public void OnSessionDisconnected()
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        

        static void Main(string[] args)
        {
            EasySocketConfig config = new EasySocketConfig.Builder()
                .SetServerGenerator<AsyncSocketServerWorker>(
                    new SocketServerWorkerConfig(), 
                    new TestBehavior())
                .AddListener(new ListenerConfig("Any", 9199, 10000, true))
                .Build();
        

            EasySocketService service = new EasySocketService(config);
        }
    }
}
