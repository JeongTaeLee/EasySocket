using System;
using EasySocket;
using EasySocket.Workers;
using EasySocket.Workers.Async;
using EasySocket.Listeners;

namespace EasySocketTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            EasySocketConfig config = new EasySocketConfig.Builder()
                .AddListener(new ListenerConfig("Any", 9199, 10000))
                .SetServerGenerator<AsyncSocketServerWorker>()
                .Build();
        

            EasySocketService service = new EasySocketService(config);
        }
    }
}
