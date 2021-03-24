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

namespace Echo.Client
{
    class Program
    {
        class TestClientBehavior : IClientBehavior
        {
            public void OnStarted(IClient client)
            {
                Console.WriteLine("Started");
            }

            public void OnStoped(IClient client)
            {
                Console.WriteLine("Stoped");
            }
            
            public void OnReceived(IClient client, IMsgInfo msgInfo)
            {
                var convertInfo = msgInfo as EchoMsgInfo;

                Console.WriteLine($"Received : {convertInfo.str}");
            }

            public void OnError(IClient client, Exception ex)
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

        internal class EchoFilter : IMsgFilter
        {
            public IMsgInfo Filter(ref SequenceReader<byte> sequence)
            {
                var buffer = sequence.Sequence.Slice(0, sequence.Length);
                sequence.Advance(sequence.Length);

                return new EchoMsgInfo(Encoding.Default.GetString(buffer));
            }

            public void Reset()
            {
            }
        }

        static CancellationTokenSource cancelationToken = new CancellationTokenSource();

        static TcpSocketClient socketClient = null;

        static async Task Main(string[] args)
        {
            socketClient = new TcpSocketClient();
            await socketClient
                .SetLoggerFactory(new NLogLoggerFactory("./NLog.config"))
                .SetMsgFilter(new EchoFilter())
                .SetSocketClientConfig(new SocketClientConfig("127.0.0.1", 9199))
                .SetClientBehavior(new TestClientBehavior())
                .StartAsync();


            while (socketClient.state == IClient.State.Running)
            {
                var input = Console.ReadLine();

                if (input == "close")
                {
                    break;
                }

                socketClient.Send(Encoding.Default.GetBytes(input));
                Console.WriteLine($"Sended : {input}");
            }

            await socketClient.StopAsync();
        }
    }
}

