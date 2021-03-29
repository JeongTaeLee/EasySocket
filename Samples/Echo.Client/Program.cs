using System;
using System.Text;
using System.Buffers;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using EasySocket.Client;
using EasySocket.Common.Protocols.MsgInfos;
using EasySocket.Common.Protocols.MsgFilters;
using EasySocket.Common.Logging;
using Echo.Client.Logging;

namespace Echo.Client
{
    class Program
    {
        class TestClientBehavior : IClientBehavior<string>
        {
            public void OnStarted(IClient<string> client)
            {
                Console.WriteLine("Started");
            }

            public void OnStoped(IClient<string> client)
            {
                Console.WriteLine("Stoped");
            }
            
            public void OnReceived(IClient<string> client, string msgInfo)
            {
                Console.WriteLine($"Received : {msgInfo}");
            }

            public void OnError(IClient<string> client, Exception ex)
            {
                Console.WriteLine("Error");
            }
        }
        
        internal class EchoMsgInfo : IMsgInfo
        {
            public string str { get; private set; }

            public EchoMsgInfo(string str)
            {
                this.str = str;
            }
        }

        internal class EchoFilter : IMsgFilter<string>
        {
            public string Filter(ref SequenceReader<byte> sequence)
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
            var socketClient = new TcpSocketClient<string>();
            await socketClient
                .SetLoggerFactory(new NLogLoggerFactory("./NLog.config"))
                .SetMsgFilter(new EchoFilter())
                .SetSocketClientConfig(new SocketClientConfig("127.0.0.1", 9199))
                .SetClientBehavior(new TestClientBehavior())
                .StartAsync();


            while (socketClient.state == ClientState.Running)
            {
                var input = Console.ReadLine();

                if (input == "close")
                {
                    break;
                }

                await socketClient.SendAsync(Encoding.Default.GetBytes(input));
                Console.WriteLine($"Sended : {input}");
            }

            await socketClient.StopAsync();
        }
    }
}

