using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasySocket.Server;
using EasySocket.Server.Listeners;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class TcpStreamPipeSocketSessionTest
    {
        [TestMethod]
        public async Task CallBackTask()
        {
            const int CONNECTOR_COUNT = 1;

            var ssns = new ConcurrentDictionary<string, TcpStreamPipeSocketSession>();

            // 서버 비헤비어 정의
            var ssnBhvr = new EventSessionBehaviour();
            ssnBhvr.onStartAfter += (ssn) =>
            {
                Assert.AreEqual(true, ssns.TryAdd(ssn.id, ssn as TcpStreamPipeSocketSession));
            };
            ssnBhvr.onStopped += (ssn) =>
            {
                Assert.AreEqual(true, ssns.TryRemove(ssn.id, out var _));
            };

            // 서버 시작
            var server = TestExtensions.CreateStringTcpServer(ssnBhvr);
            await server.StartAsync(new ListenerConfig("Any", 9199, 1000));

            // // 클라이언트 비헤비어 정의
            // var clntBhvr = new EventClientBehavior();
            
            // 다수의 Client 시작 
            var clients = await TestExtensions.ConnectTcpSocketClient("127.0.0.1", 9199, CONNECTOR_COUNT);

            // 클라이언트 갯수 체크.
            Assert.AreEqual(CONNECTOR_COUNT, ssns.Count);

            // 클라이언트 모두 종료
            var stopTasks = new List<Task>();
            foreach (var client in clients)
            {
                await client.StopAsync();
            }

            await Task.Delay(2000);

            // 종류 후 클라이언트 갯수 체크
            Assert.AreEqual(0, ssns.Count);

            // 서버 종료
            await server.StopAsync();            
        }
    }
}