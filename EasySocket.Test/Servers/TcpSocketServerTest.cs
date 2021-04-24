
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Server.Listeners;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class TcpSocketServerTest
    {
     
        // 기본 연결 테스트.
        [TestMethod]
        public async Task ConnectTest()
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
                clients.RemoveAt(0);
                await clients[0].StopAsync();
                clients.RemoveAt(0);
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

        // 리스너 기능 테스트.
        [TestMethod]
        public async Task ListenerTest()
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
            await server.StopAllListenerAsync();

            // 연결 실패 확인.
            await Assert.ThrowsExceptionAsync<SocketException>(async () => { await TestExtensions.ConnectTcpSocketClient("127.0.0.1", curPort);});
            await Assert.ThrowsExceptionAsync<SocketException>(async () => { await TestExtensions.ConnectTcpSocketClient("127.0.0.1", firstPort);});
            await Assert.ThrowsExceptionAsync<SocketException>(async () => { await TestExtensions.ConnectTcpSocketClient("127.0.0.1", secondPort);});

            // 사이드 이펙트 확인(클라이언트 연결 유지
            Assert.AreEqual(clientCount, server.sessionCount);

            // 테스트 끝 서버 종료
            await server.StopAsync();
        }
    }
}
