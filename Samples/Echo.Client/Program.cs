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

        // P/Invoke:
        private enum StdHandle { Stdin = -10, Stdout = -11, Stderr = -12 };
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(StdHandle std);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hdl);

        static CancellationTokenSource cancelationToken = new CancellationTokenSource();

        static TcpSocketClient socketClient = null;

        static async Task Main(string[] args)
        {
            socketClient = new TcpSocketClient();
            socketClient
                .SetLoggerFactory(new NLogLoggerFactory("./NLog.config"))
                .SetMsgFilter(new EchoFilter())
                .SetSocketClientConfig(new SocketClientConfig("127.0.0.1", 9199))
                .SetClientBehavior(new TestClientBehavior())
                .Start();


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

        static void OnError(IClient client, Exception ex)
        {
            Console.WriteLine("OnError");
        }

        static long OnReceived(IClient client, ref ReadOnlySequence<byte> sequence)
        {
            Console.WriteLine("OnReceived");
            return sequence.Length;
        }

        static void OnStop(IClient client)
        {
            Console.WriteLine("OnStop");
        }

        static async Task ProcessSend(Socket socket)
        {
            await Task.Yield();

            while (!cancelationToken.IsCancellationRequested)
            {
                var inputStr = Console.ReadLine();
                if (inputStr == "Close")
                {
                    cancelationToken?.Cancel();
                    break;
                }

                if (cancelationToken.IsCancellationRequested)
                {
                    break;
                }

                var sendByte = Encoding.Default.GetBytes(inputStr);
                var sendLength = await socket.SendAsync(sendByte, SocketFlags.None);

                Console.WriteLine($"Sended({sendLength})");
            }
        }
        
        static async Task ProcessReceive(Socket socket)
        {
            var networkStream = new NetworkStream(socket);
            var pipeReader = PipeReader.Create(networkStream);

            try
            {

                while (!cancelationToken.IsCancellationRequested)
                {

                    var result = await pipeReader.ReadAsync(cancelationToken.Token);
                    var buffer = result.Buffer;

                    long readLength = buffer.Length;

                    try
                    {
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        var receiveStr = Encoding.Default.GetString(buffer);
                        Console.WriteLine(receiveStr);

                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        pipeReader.AdvanceTo(buffer.GetPosition(readLength));
                    }
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                if (!cancelationToken.IsCancellationRequested)
                {
                    cancelationToken.Cancel();
                }

                await pipeReader.CompleteAsync();
                networkStream?.Close();
            }
        }
    }
}

