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
        public async Task SessionConnectTest()
        {
            //
            var ssnBhvr = new EventSessionBehaviour();

            //
            int curPort = 9199;
            
            // 서버 생성.
            var server = TestExtensions.CreateTcpSocketServer(ssnBhvr);

            // 서버 시작
            await server.StartAsync(new ListenerConfig("127.0.0.1", curPort, 100));

            // 연결/종료 테스트
            {
                // 클라이언트 연결
                var client = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);

                // 연결 확인
                await Task.Delay(100);
                Assert.AreEqual(1, server.sessionCount);

                await client.StopAsync();
                
                // 연결 종료
                await Task.Delay(100);
                Assert.AreEqual(0, server.sessionCount);
            }

            // 다중 연결/종료 테스트
            {
                var connectClientCount = 100;

                var clients = await TestExtensions.ConnectTcpSocketClients("127.0.0.1", curPort, connectClientCount);

                // 연결 확인
                await Task.Delay(100);
                Assert.AreEqual(connectClientCount, server.sessionCount);

                // 두개 종료
                await clients[0].StopAsync();
                await clients[1].StopAsync();
                connectClientCount -= 2;

                // 종료 후 확인
                await Task.Delay(100);
                Assert.AreEqual(connectClientCount, server.sessionCount);

                // 모두 종료
                await Task.WhenAll(clients.Select(client => client.StopAsync()));

                // 모두 종료 후 확인
                await Task.Delay(100);
                Assert.AreEqual(0, server.sessionCount);
            }

            await server.StopAsync();
        }

        [TestMethod]
        public async Task ListenerAddRemoveTest()
        {
            //
            var ssnBhvr = new EventSessionBehaviour();

            //
            int curPort = 9199;
            int clientCount = 0;
            
            // 서버 생성.
            var server = TestExtensions.CreateTcpSocketServer(ssnBhvr);

            // 서버 시작
            await server.StartAsync(new ListenerConfig("127.0.0.1", curPort, 100));

            // 클라이언트 연결
            await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);
            clientCount++;

            // 클라이언트 연결 확인
            await Task.Delay(100);
            Assert.AreEqual(clientCount, server.sessionCount);

            // 리스너 종료
            await server.StopListenerAsync(curPort);

            // 사이드 이펙트 확인(클라이언트 연결 유지
            Assert.AreEqual(clientCount, server.sessionCount);

            // 끊어진 리스너에 연결 시도.
            await Assert.ThrowsExceptionAsync<SocketException>(async () =>
            {
                await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);
            });

            // 연결되었는지 확인
            Assert.AreEqual(clientCount, server.sessionCount);

            // 다른 포트로 리스너 오픈
            curPort = 10020;
            await server.StartListenerAsync(new ListenerConfig("127.0.0.1", curPort, 100));
            clientCount++;

            // 오픈된 리스너에 클라이언트 연결 시도
            await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);

            // 클라이언트 연결 확인
            await Task.Delay(100);
            Assert.AreEqual(clientCount, server.sessionCount);

            // 다중 연결 테스트
            int firstPort = 10021;
            int secondPort = 10022;

            await server.StartListenersAsync(new List<ListenerConfig>
            {
                new ListenerConfig("127.0.0.1", firstPort, 100),
                new ListenerConfig("127.0.0.1", secondPort, 100)
            });

            // 각각 연결 테스트
            await TestExtensions.ConnectTcpSocketClient("127.0.0.1", firstPort);
            await TestExtensions.ConnectTcpSocketClient("127.0.0.1", secondPort);
            clientCount += 2;

            // 클라이언트 연결 확인
            await Task.Delay(100);
            Assert.AreEqual(clientCount, server.sessionCount);

            // 모두 리스너 모두 스톱
            await server.StopAllListenersAsync();

            // 연결 실패 확인.
            await Assert.ThrowsExceptionAsync<SocketException>(async () => { await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);});
            await Assert.ThrowsExceptionAsync<SocketException>(async () => { await TestExtensions.ConnectTcpSocketClient("127.0.0.1", firstPort);});
            await Assert.ThrowsExceptionAsync<SocketException>(async () => { await TestExtensions.ConnectTcpSocketClient("127.0.0.1", secondPort);});

            // 사이드 이펙트 확인(클라이언트 연결 유지
            Assert.AreEqual(clientCount, server.sessionCount);

            // 테스트 끝 서버 종료
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
