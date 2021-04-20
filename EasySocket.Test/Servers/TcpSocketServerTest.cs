using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using EasySocket.Server.Listeners;
using EasySocket.Test.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class TcpSocketServerTest
    {
        [TestMethod]
        public async Task ListenerAddRemoveTest()
        {
            const int CONNECTE_COUNT = 100;

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

            // 서버 생성.
            var server = TestExtensions.CreateTcpSocketServer(ssnBhvr);

            int curPort = 9199;

            // 서버 시작
            await server.StartAsync(new ListenerConfig("127.0.0.1", curPort, 100));

            {
                // 연결 
                var client = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);

                // 대기
                await Task.Delay(100);

                // 리스너 종료
                await server.StopListenerAsync(curPort);

                // 체크
                Assert.AreEqual(server.sessionCount, 1);
                Assert.AreEqual(server.sessionCount, 1);

                // 세션 종료
                await client.StopAsync();

                // 대기
                await Task.Delay(100);

                // 종료 후 세션 카운트 체크
                Assert.AreEqual(server.sessionCount, 0);
                Assert.AreEqual(server.sessionCount, 0);

                // 종료 후 연결(예외 발생해야함)
                await Assert.ThrowsExceptionAsync<SocketException>(async () =>
                {
                    await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);
                });
            }

            {
                // 다시 시작
                await server.StartListenerAsync(new ListenerConfig("127.0.0.1", curPort, 100));

                // 연결 
                var client = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);

                // 대기
                await Task.Delay(100);

                // 체크
                Assert.AreEqual(server.sessionCount, 1);
                
                // 리스너 종료
                await server.StopListenerAsync(curPort);

                // 세션 종료
                await client.StopAsync();

                // 대기
                await Task.Delay(100);

                // 체크
                Assert.AreEqual(server.sessionCount, 0);
            }

            {
                var secondPort = 9192;

                // 두개의 리스너 실행
                await server.StartListenersAsync(new List<ListenerConfig>()
                {
                    new ListenerConfig("127.0.0.1", curPort, 100),
                    new ListenerConfig("127.0.0.1", secondPort, 100),
                });

                // 두개 포트 연결 모드 시도
                var client = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);
                var secondClient = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", secondPort);

                // 대기
                await Task.Delay(100);

                // 접속 후 체크
                Assert.AreEqual(server.sessionCount, 2);

                // 종료 대기
                await Task.WhenAll(client.StopAsync(), secondClient.StopAsync());

                // 대기
                await Task.Delay(100);

                // 종료 후 체크
                Assert.AreEqual(server.sessionCount, 0);

                // 첫번째 포트 종료
                await server.StopListenerAsync(curPort);

                // 첫번째 포트 접속 실패 테스트
                await Assert.ThrowsExceptionAsync<SocketException>(async () =>
                {
                    await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);
                });

                // 열려있는 두번째 포트 접속 테스트
                secondClient = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", secondPort);

                // 대기
                await Task.Delay(100);

                // 접속 후 체크
                Assert.AreEqual(server.sessionCount, 1);

                // 종료 대기
                await secondClient.StopAsync();

                // 대기
                await Task.Delay(100);

                // 종료 후 체크
                Assert.AreEqual(server.sessionCount, 0);

                // 두 번째 포트 종료
                await server.StopListenerAsync(secondPort);

                // 두번째 포트에 재연결 실패 테스트
                await Assert.ThrowsExceptionAsync<SocketException>(async () =>
                {
                    await TestExtensions.ConnectTcpSocketClient("127.0.0.1", secondPort);
                });
            }

            await server.StopAsync();
        }

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
            var clients = await TestExtensions.ConnectTcpSocketClients("127.0.0.1", freeLocalPort, CONNECTOR_COUNT);

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
