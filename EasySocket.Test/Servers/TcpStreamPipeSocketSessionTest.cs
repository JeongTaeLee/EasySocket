using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasySocket.Server.Listeners;
using EasySocket.Server;
using EasySocket.Common.Protocols.Factories;
using EasySocket.Test.Components;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class TcpStreamPipeSocketSessionTest
    {
        [TestMethod]
        public async Task BehaviourCallbackTest()
        {
            int port = 9199;
            int serverOnErrorCount = 0;
            int startBeforeCallCount = 0;
            int startAfterCallCount = 0;
            int stoppedCallCount = 0;
            int errorCallCount = 0;

            EventSessionBehaviour ssnBhvr  = new EventSessionBehaviour();
            ssnBhvr.onStartBefore += (ssn) => { Interlocked.Increment(ref startBeforeCallCount); };
            ssnBhvr.onStartAfter += (ssn) => { Interlocked.Increment(ref startAfterCallCount); };
            ssnBhvr.onStopped += (ssn) => { Interlocked.Increment(ref stoppedCallCount); };
            ssnBhvr.onError += (ex) => 
            { 
                Console.WriteLine(ex);
                Interlocked.Increment(ref errorCallCount); 
            };

            // 서버 시작.
            var server = new TcpStreamPipeSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr);
                })
                .SetOnError((ssn, ex) =>
                {
                    ++serverOnErrorCount;
                });

            // 서버 시작
            await server.StartAsync(new ListenerConfig("127.0.0.1", port, 1000));
            
            // 다수의 클라이언트 연결
            int connectCount = 50;
            var clients = await TestExtensions.ConnectTcpSocketClients("127.0.0.1", port, connectCount);

            // 연결 콜백 확인.
            await Task.Delay(100);
            Assert.AreEqual(connectCount, startBeforeCallCount);
            Assert.AreEqual(connectCount, startAfterCallCount);

            // 여러개 종료
            int stopCount = 5;
            for (int stopIndex = 0; stopIndex < stopCount; ++stopIndex)
            {
                await clients[0].StopAsync();
                clients.RemoveAt(0);
            }

            // 종료 확인
            await Task.Delay(100);
            Assert.AreEqual(stopCount, stoppedCallCount);
            Assert.AreEqual(connectCount - stopCount, server.sessionCount ); // 남아있는 클라이언트 확인

            // 모두 종료
            await Task.WhenAll(clients.Select(async (client) => { await client.StopAsync(); }));

            // 종료 콜백 확인
            await Task.Delay(100);
            Assert.AreEqual(connectCount, stoppedCallCount);
            Assert.AreEqual(0, server.sessionCount); // 남아있는 클라이언트 확인

            // 에러 발생 확인
            Assert.AreEqual(0, errorCallCount);
        
            // 종료
            await server.StopAsync();

            // 서버 에러 확인
            Assert.AreEqual(0, serverOnErrorCount);
        }
    }
}