using System;
using System.Threading.Tasks;
using EasySocket.Client;
using EasySocket.Common.Protocols.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;
using EasySocket.Test.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Clients
{
    [TestClass]
    public class TcpStreamPipeSocketClientTest
    {
        [TestMethod]
        public async Task CloseFromServerTest()
        {
            //
            ISession cacheSsn = null; ;
            var onSrveStartBeforeCount = 0;
            var onSrveStartAfterCount = 0;
            var onSrveStoppedCount = 0;
            var onSrveErrorCount = 0;

            var eventSsnBehaviour = new EventSessionBehaviour();
            eventSsnBehaviour.onStartBefore += (ssn) => { ++onSrveStartBeforeCount; };
            eventSsnBehaviour.onStartAfter += (ssn) => { ++onSrveStartAfterCount; cacheSsn = ssn; };
            eventSsnBehaviour.onStopped += (ssn) => { ++onSrveStoppedCount; };

            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(eventSsnBehaviour);
                })
                .SetOnError((ssn, ex) =>
                {
                    ++onSrveErrorCount;
                });


            // 서버 연결
            await server.StartAsync(new ListenerConfig("127.0.0.1", 9199, 1000));

            //
            var onClntStarted = 0;
            var onClntStopped = 0;
            var onClntErrorCount = 0;

            var eventClntBehaviour = new EventClientBehavior();
            eventClntBehaviour.onStarted += (clnt) => { ++onClntStarted; };
            eventClntBehaviour.onStopped += (clnt) => { ++onClntStopped; };
            eventClntBehaviour.onError += (clnt, ex) => { ++onClntErrorCount; };

            // 클라이언트 연결
            var client = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", 9199, eventClntBehaviour);

            await Task.Delay(100);

            //
            Assert.AreEqual(1, onClntStarted);
            Assert.AreEqual(1, onSrveStartBeforeCount);
            Assert.AreEqual(1, onSrveStartAfterCount);

            // 세션 종료
            await cacheSsn.StopAsync();

            await Task.Delay(100);

            //
            Assert.AreEqual(1, onClntStopped);
            Assert.AreEqual(1, onSrveStoppedCount);

            Assert.AreEqual(0, server.sessionCount);
            Assert.AreEqual(ClientState.Stopped, client.state);

            //
            Assert.AreEqual(0, onSrveErrorCount);
            Assert.AreEqual(0, onClntErrorCount);

            //
            await server.StopAsync();
        }
    }
}
