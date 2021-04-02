using System;
using System.Threading.Tasks;
using EasySocket.Common.Protocols.MsgFilters.Factories;
using EasySocket.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class TcpSocketServerTest
    {
        [TestMethod]
        public async Task StartTest()
        {
            var testServer = new TcpSocketServer<string>();

            // 초기 상태 테스트.
            Assert.AreEqual(testServer.state, ServerState.None);
            
            // 초기화 없이 서버를 시작 할 때 테스트
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await testServer.StartAsync();
            });

            // 실패 했을 경우 상태가 변경되면 안됨.
            Assert.AreEqual(testServer.state, ServerState.None);

            // NULL 처리 테스트
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                testServer.SetLoggerFactory(null);
            });

            // NULL 처리 테스트
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                testServer.SetMsgFilterFactory(null);
            });

            // NULL 처리 테스트
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                testServer.SetSessionConfigrator(null);
            });

            //정상 상황 테스트.
            var startTask = testServer
                    .AddListener(new Server.Listeners.ListenerConfig("127.0.0.1", 9199, 1000))
                    .SetLoggerFactory(new ConsoleLoggerFactory())
                    .SetMsgFilterFactory(new DefaultMsgFilterFactory<StringMsgFilter, string>())
                    .SetSessionConfigrator((ssn) =>
                    {
                        //ssn.SetSessionBehavior
                    })
                    .StartAsync().ConfigureAwait(false);

            // 시작중인 상태 체크 (서버가 바로 시작하면..)
            // Assert.AreEqual(testServer.state, ServerState.Starting);

            await startTask;

            // 서버 진행 중..
            Assert.AreEqual(testServer.state, ServerState.Running);

            var stopTask = testServer.StopAsync();

            // 이것도 시작중인 상태와 마찬가지 문제가 있음 ..
            // Assert.AreEqual(testServer.state, ServerState.Stopping);

            await stopTask;

            // 종료 상태 체크.
            Assert.AreEqual(testServer.state, ServerState.Stopped);

            // 서버는 재시작되면 안됨..
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await testServer.StartAsync();
            });
        }
    
        
    }
}