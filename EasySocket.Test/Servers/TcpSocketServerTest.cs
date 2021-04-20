using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Test.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class TcpSocketServerTest
    {
        // [TestMethod]
        // public async Task ServerStartTest()
        // {
        //     var server = new TcpSocketServer();

        //     // 초기 상태 테스트.
        //     Assert.AreEqual(server.state, ServerState.None);
            
        //     //정상 상황 테스트.
        //     var freeLocalPort = TestExtensions.GetFreePort("127.0.0.1");
        //     var startTask = server
        //         .AddListener(new Server.Listeners.ListenerConfig("127.0.0.1", freeLocalPort, 1000))
        //         .SetLoggerFactory(new ConsoleLoggerFactory())
        //         .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
        //         .SetSessionConfigrator((ssn) =>
        //         {
       
        //         })
        //         .StartAsync().ConfigureAwait(false);

        //     // 시작중인 상태 체크 -> Async 지만 바로 시작할 수도 있으니 Running 까지 Pass 로 처리한다.
        //     Assert.IsTrue(server.state == ServerState.Starting || server.state == ServerState.Running);

        //     await startTask;

        //     // 서버 진행 중..
        //     Assert.AreEqual(server.state, ServerState.Running);

        //     var stopTask = server.StopAsync();

        //     // 종료중인 상태 체크 -> Async 지만 바로 종료될 수 있으니 Stopped 까지 Pass 로 처리한다.
        //     Assert.IsTrue(server.state == ServerState.Stopping || server.state == ServerState.Stopped);

        //     await stopTask;

        //     // 종료 상태 체크.
        //     Assert.AreEqual(server.state, ServerState.Stopped);

        //     // 서버는 재시작되면 안됨..
        //     await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        //     {
        //         await server.StartAsync();
        //     });
        // }
        
        [TestMethod]
        public async Task SessionBehaviourCallbackTest()
        {
            const int CONNECTOR_COUNT = 10;

            int createdSessionCount = 0;
            int onStartBeforeCalled = 0;
            int onStartAfterCalled = 0;
            int onStoppedCalled = 0;
            int onReceivedCalled = 0;
            int onErrorCalled = 0;

            var ssnBhvr = new EventSessionBehaviour();
            ssnBhvr.onStartBefore += (inSsn) => Interlocked.Increment(ref onStartBeforeCalled);
            ssnBhvr.onStartAfter += (inSsn) => Interlocked.Increment(ref onStartAfterCalled);
            ssnBhvr.onStopped += (inSsn) => Interlocked.Increment(ref onStoppedCalled);
            ssnBhvr.onReceived += (inSsn) => Interlocked.Increment(ref onReceivedCalled);
            ssnBhvr.onError += (inSsn) => Interlocked.Increment(ref onErrorCalled);

            // 서버 실행 준비.
            var freeLocalPort = TestExtensions.GetFreePort("127.0.0.1");
            var server = new TcpSocketServer()
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr);

                    // 세션 개수 카운팅을 올려준다.
                    Interlocked.Increment(ref createdSessionCount);
                });

            await server.StartAsync(new Server.Listeners.ListenerConfig("127.0.0.1", freeLocalPort, 1000));

            // 서버 진행 중..
            Assert.AreEqual(server.state, ServerState.Running);

            // 클라이언트 연결
            var clients = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", freeLocalPort, CONNECTOR_COUNT);

            // 대기
            await Task.Delay(1000);

            // 생성된 세션 수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(createdSessionCount, CONNECTOR_COUNT);

            // 시작 시 콜백 호출 횟수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(onStartBeforeCalled, CONNECTOR_COUNT);
            Assert.AreEqual(onStartAfterCalled, CONNECTOR_COUNT);

            // "Hello" 문자열을 모두 보낸다.
            var sendingMsg = Encoding.Default.GetBytes("Hello");
            await Task.WhenAll(clients.Select(async client => await client.SendAsync(sendingMsg)));
            
            // 대기
            await Task.Delay(1000); // TODO - 보내는 Task 를 다 기다렸지만.. 바로 Counting이 되지 않는다.

            // 받은 횟수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(onReceivedCalled, CONNECTOR_COUNT);

            // 모든 클라이언트 종료.
            await Task.WhenAll(clients.Select(async client => await client .StopAsync()));

            // 대기
            await Task.Delay(1000);

            // 종료 시 콜백 호출 횟수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(onStoppedCalled, CONNECTOR_COUNT);

            // 서버를 끈다.
            await server.StopAsync();

            // 에러 횟수는 합계 0 이 되어야 한다.
            Assert.AreEqual(onErrorCalled, 0);
        }
    }
}
