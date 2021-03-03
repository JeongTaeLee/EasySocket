using System;
using EasySocket;
using EasySocket.Workers;
using EasySocket.Workers.Async;
using EasySocket.Listeners;
using EasySocket.Behaviors;
using System.Net.Sockets;

namespace EasySocketTestConsole
{
    class TestServerBehavior : IServerBehavior
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

    class TestSessionBehavior : ISessionBehavior
    {
        public void OnClosed()
        {
            throw new NotImplementedException();
        }

        public void OnStarted()
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var service = new EasySocketService()
                .SetSocketServer<AsyncSocketServerWorker>()
                .SetSocketServerConfigrator((socketServer) =>
                {
                    socketServer
                        .AddListener(new ListenerConfig("Any", 9199, 100000, true))
                        .SetServerBehavior(new TestServerBehavior())
                        .SetServerConfig(new SocketServerWorkerConfig())
                        ;
                })
                .SetSocketSessionConfigrator((socketSession) =>
                {
                    socketSession
                        .SetSessionBehavior(new TestSessionBehavior())
                        ;
                });

            service.Start();
        }
    }
}
