using System.Threading.Tasks;
using EasySocket.Server.Listeners;
using EasySocket.Test.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasySocket.Test.Servers
{
    [TestClass]
    public class TcpSocketListenerTest
    {
        [TestMethod]
        [Timeout(10000)]
        public async Task RestartTest()
        {
            int restartCount = 10;

            TcpSocketListener listener = new TcpSocketListener();

            for (int index = 0; index < restartCount; ++index)
            {
                await listener.StartAsync(new ListenerConfig("Any", 9199, 100)
                    ,new ConsoleLoggerFactory().GetLogger(typeof(TcpSocketListenerTest)));
                    
                await listener.StopAsync();
            }
        }
    }
}