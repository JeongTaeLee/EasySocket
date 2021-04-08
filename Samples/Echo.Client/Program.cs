using System;
using System.Text;
using System.Buffers;
using System.Threading.Tasks;
using System.Threading;
using EasySocket.Client;
using EasySocket.Common.Protocols.MsgFilters;
using Echo.Client.Logging;
using System.Collections.Generic;

namespace Echo.Client
{
    class Program
    {
        class TestClientBehavior : IClientBehavior
        {
            private int i = 0;

            public void OnStarted(IClient client)
            {
                lock(this)
                {
                    Console.WriteLine("Started");
                    Console.WriteLine(i);
                    ++i;
                }
            }

            public void OnStoped(IClient client)
            {
                Console.WriteLine("Stoped");
            }
            
            public void OnReceived(IClient client, object msgInfo)
            {
                Console.WriteLine($"Received : {msgInfo}");
            }

            public void OnError(IClient client, Exception ex)
            {
                Console.WriteLine("Error");
            }
        }
        internal class EchoFilter : IMsgFilter
        {
            public object Filter(ref SequenceReader<byte> sequence)
            {
                var buffer = sequence.Sequence.Slice(0, sequence.Length);
                sequence.Advance(sequence.Length);

                return Encoding.Default.GetString(buffer);
            }

            public void Reset()
            {
            }
        }

        static CancellationTokenSource cancelationToken = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            var sessionBehavior = new TestClientBehavior();

            var lst = new List<TcpSocketClient>();
            var tasks = new List<Task>();
            for (int index = 0; index < 100; ++index)
            {
                var socketClient = new TcpSocketClient();
                lst.Add(socketClient);
                tasks.Add(socketClient
                    .SetLoggerFactory(new NLogLoggerFactory("./NLog.config"))
                    .SetMsgFilter(new EchoFilter())
                    .SetSocketClientConfig(new SocketClientConfig("127.0.0.1", 9199))
                    .SetClientBehavior(sessionBehavior)
                    .StartAsync());
            }

            await Task.WhenAll(tasks);

            while (true)
            {
                var inputStr =Console.ReadLine();
                if (inputStr == "close")
                {
                    break;
                }
            }

            tasks.Clear();

            foreach (var client in lst)
            {
                tasks.Add(client.StopAsync());
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("Done");
        }
    }
}

