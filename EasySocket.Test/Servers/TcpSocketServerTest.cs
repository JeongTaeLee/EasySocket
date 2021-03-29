using System;
using System.Threading.Tasks;
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

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                // 초기화 없이 서버를 시작 할 때 테스트
                await testServer.StartAsync();
            });

            // 실패 했을 경우 상태가 변경되면 안됨.
            Assert.AreEqual(testServer.state, ServerState.None);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                // NULL 처리 테스트
                testServer.SetLoggerFactroy(null);
            });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                // NULL 처리 테스트
                testServer.SetMsgFilterFactory(null);
            });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                // NULL 처리 테스트
                testServer.SetSessionConfigrator(null);
            });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                // NULL 처리 테스트
                testServer.SetServerBehavior(null);
            });

            // var server = testServer
            //         .SetMsgFilterFactory(new )

            // 정상 상황 테스트.
            // var startTask  = testServer
            //         .SetLoggerFactroy(null)
            //         .SetMsgFilterFactory(null)
            //         .SetSessionConfigrator(null)
            //         .SetServerBehavior(null)
            //         .StartAsync();

            // // TODO : 서버가 빠르게 시작 완료되었을 경우 생각해보기.
            // Assert.AreEqual(testServer, ServerState.Starting);
        }
    }
}