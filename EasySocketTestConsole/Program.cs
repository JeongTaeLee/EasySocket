using System;
using EasySocket;
using EasySocket.Workers;
using EasySocket.Workers.Async;
using EasySocket.Listeners;
using EasySocket.Behaviors;
using EasySocket.Logging;
using EasySocket.Protocols.Filters;
using EasySocket.Protocols.Filters.Factories;
using EasySocket.Protocols.MsgInfos;
using System.Buffers;

namespace EasySocketTestConsole
{
    class TestServerBehavior : IServerBehavior
    {
        public void OnError(Exception ex)
        {
            throw new NotImplementedException();
        }

        public void OnSessionConnected(ISocketSessionWorker session)
        {
            throw new NotImplementedException();
        }

        public void OnSessionDisconnected(ISocketSessionWorker session)
        {
            throw new NotImplementedException();
        }
    }

    class TestSessionBehavior : ISessionBehavior
    {
        public void OnStarted()
        {
            throw new NotImplementedException();
        }
        public void OnClosed()
        {
            throw new NotImplementedException();
        }

        public void OnReceived(IMsgInfo msg)
        {
            throw new NotImplementedException();
        }
        
        public void OnError(Exception ex)
        {
            throw new NotImplementedException();
        }
    }

    class TestMsgFilter : IMsgFilter
    {
        public IMsgInfo Filter(ref SequenceReader<byte> sequence)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var service = new EasySocketService()
                .SetLoggerFactroy(new NLogLoggerFactory("NLog.config"))
                .SetSocketServer<AsyncSocketServerWorker>()
                .SetSocketServerConfigrator((socketServer) =>
                {
                    socketServer
                        .AddListener(new ListenerConfig("Any", 9199, 100000, true))
                        .SetMsgFilterFactory(new DefaultMsgFilterFactory<TestMsgFilter>())
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

            while (true)
            {
            }
        }
    }
}
