using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Test.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class SocketServerTest
    {
        [TestMethod]
        [Timeout(10_000)]
        public async Task Test_TcpSocketServer_RunAndStop()
        {
            var server = new TcpSocketServer();

            // 초기 상태 테스트.
            Assert.AreEqual(ServerState.None, server.state);
            
            // 초기화 없이 서버를 시작 할 때 테스트
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await server.StartAsync();
            });

            // 실패 했을 경우 상태가 변경되면 안됨.
            Assert.AreEqual(ServerState.None, server.state);

            // NULL 처리 테스트
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                server.SetLoggerFactory(null);
            });

            // NULL 처리 테스트
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                server.SetMsgFilterFactory(null);
            });

            // NULL 처리 테스트
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                server.SetSessionConfigrator(null);
            });

            //정상 상황 테스트.
            var freeLocalPort = TestHelper.GetLocalFreePort();
            var startTask = server
                .AddListener(new Server.Listeners.ListenerConfig("127.0.0.1", freeLocalPort, 1000))
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
       
                })
                .StartAsync().ConfigureAwait(false);

            // 시작중인 상태 체크 -> Async 지만 바로 시작할 수도 있으니 Running 까지 Pass 로 처리한다.
            Assert.IsTrue(server.state == ServerState.Starting || server.state == ServerState.Running);

            await startTask;

            // 서버 진행 중..
            Assert.AreEqual(ServerState.Running, server.state);

            var stopTask = server.StopAsync();

            // 종료중인 상태 체크 -> Async 지만 바로 종료될 수 있으니 Stopped 까지 Pass 로 처리한다.
            Assert.IsTrue(server.state == ServerState.Stopping || server.state == ServerState.Stopped);

            await stopTask;

            // 종료 상태 체크.
            Assert.AreEqual(ServerState.Stopped, server.state);

            // 서버는 재시작되면 안됨..
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await server.StartAsync();
            });
        }

        [TestMethod]
        [Timeout(10_000)]
        public async Task Test_TcpSocketServer_MultiListener()
        {
            const int LISTENER_COUNT = 10;

            int acceptCount = 0;

            // 서버 실행 준비.
            var server = new TcpSocketServer();
            var ports = new int[LISTENER_COUNT];

            // 10 개의 Listener 를 추가한다.
            for (int i = 0; i < 10; ++i)
            {
                ports[i] = TestHelper.GetLocalFreePort();
                server.AddListener(new Server.Listeners.ListenerConfig("127.0.0.1", ports[i], 1000));
            }

            server.SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    Interlocked.Increment(ref acceptCount);
                });

            await server.StartAsync();

            // 테스트용 커넥터 (임시 Client) 를 만든다.
            Connector[] connectors = Enumerable.Range(0, LISTENER_COUNT)
                .Select(x => new Connector(new IPEndPoint(IPAddress.Parse("127.0.0.1"), ports[x])))
                .ToArray();

            await Task.WhenAll(connectors.Select(x => x.ConnectAsync()));
            await Task.Delay(1000); // TODO - 연결하는 Task 를 다 기다렸지만.. 바로 Counting이 되지 않는다.

            // 각 Listener에 Connector를 하나씩 매칭해 연결한 후
            // 잘 연결되었는지 카운터를 체크한다.
            Assert.AreEqual(LISTENER_COUNT, acceptCount);
        }

        [TestMethod]
        [Timeout(10_000)]
        public async Task Test_SessionBehaviourCallbacks()
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
            var freeLocalPort = TestHelper.GetLocalFreePort();
            var server = new TcpSocketServer()
                .AddListener(new Server.Listeners.ListenerConfig("127.0.0.1", freeLocalPort, 1000))
                .SetLoggerFactory(new ConsoleLoggerFactory())
                .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter>())
                .SetSessionConfigrator((ssn) =>
                {
                    ssn.SetSessionBehaviour(ssnBhvr);

                    // 세션 개수 카운팅을 올려준다.
                    Interlocked.Increment(ref createdSessionCount);
                });

            await server.StartAsync();

            // 서버 진행 중..
            Assert.AreEqual(ServerState.Running, server.state);


            // 테스트용 커넥터 (임시 Client) 를 만든다.
            Connector[] connectors = Enumerable.Range(0, CONNECTOR_COUNT)
                .Select(x => new Connector(new IPEndPoint(IPAddress.Parse("127.0.0.1"), freeLocalPort)))
                .ToArray();

            // 커넥터를 모두 연결시킨다
            await Task.WhenAll(connectors.Select(x => x.ConnectAsync()));
            await Task.Delay(1000); // TODO - 연결하는 Task 를 다 기다렸지만.. 바로 Counting이 되지 않는다.

            // 생성된 세션 수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(CONNECTOR_COUNT, createdSessionCount);

            // 시작 시 콜백 호출 횟수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(CONNECTOR_COUNT, onStartBeforeCalled);
            Assert.AreEqual(CONNECTOR_COUNT, onStartAfterCalled);


            // "Hello" 문자열을 모두 보낸다.
            await Task.WhenAll(connectors.Select(x => x.SendStringAsync("Hello")));
            await Task.Delay(1000); // TODO - 보내는 Task 를 다 기다렸지만.. 바로 Counting이 되지 않는다.

            // 받은 횟수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(CONNECTOR_COUNT, onReceivedCalled);


            // 서버를 끈다.
            await server.StopAsync();

            // 종료 시 콜백 호출 횟수는 합계 CONNECTOR_COUNT 이 되어야 한다.
            Assert.AreEqual(CONNECTOR_COUNT, onStoppedCalled);

            // 에러 횟수는 합계 0 이 되어야 한다.
            Assert.AreEqual(0, onErrorCalled);
        }
    }
}
