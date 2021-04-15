using EasySocket.Server.Listeners;
using EasySocket.Test.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class ListenerTest
    {
        [TestMethod]
        [Timeout(10_000)]
        public async Task Test_LoopTcpListenerOpenAndClose()
        {
            // 서버 실행 준비.
            var listener = new TcpSocketListener();
            var config = TestHelper.GetLocalListenerConfig();
            var logger = new ConsoleLogger();

            // 10번 연속해서 Start & Stop 을 반복한다.
            // 정상적으로 작동해야 한다
            for (int i = 0; i < 10; ++i)
            {
                await listener.StartAsync(config, logger);
                await listener.StopAsync();
            }
        }
    }
}
