using System;
using System.Text;
using System.Buffers;
using System.Threading.Tasks;
using System.Threading;
using EasySocket.Client;
using EasySocket.Common.Protocols.MsgFilters;
using Echo.Client.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace Echo.Client
{
    class EchoFilter : FixedHeaderMsgFilter
    {
        public EchoFilter()
            : base(4)
        {

        }

        protected override int ParseBodySizeFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            return BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
        }

        protected override object ParseMsgInfo(ref ReadOnlySequence<byte> headerSeq, ref ReadOnlySequence<byte> bodySeq)
        {
            return Encoding.Default.GetString(bodySeq);
        }
    }

    class MyClient : IClientBehavior
    {
        TcpSocketClient client = null;

        int myIndex = -1;

        Stopwatch stopWatch = new Stopwatch();

        Action<long> recordPing = null;

        CancellationTokenSource tokenSource = null;

        public MyClient(int clntIdx, Action<long> rcrdPng, CancellationTokenSource tknSrc)
        {
            myIndex = clntIdx;
            recordPing = rcrdPng;
            tokenSource = tknSrc;
        }

        public async Task StartAsync()
        {
            client = new TcpSocketClient();
            await client
                .SetLoggerFactory(new NLogLoggerFactory("./NLog.config"))
                .SetMsgFilter(new EchoFilter())
                .SetSocketClientConfig(new SocketClientConfig("127.0.0.1", 9199))
                .SetClientBehavior(this)
                .StartAsync();
        }

        public async Task StopAsync()
        {
            await client.StopAsync();
        }

        public void OnStarted(IClient client)
        {
            Console.WriteLine($"started client : index({myIndex})");
        }

        public void OnStoped(IClient client)
        {
            Console.WriteLine($"stopped client : index({myIndex})");
        }
        
        public void OnReceived(IClient client, object msgFilter)
        {
            var str = msgFilter.ToString();

            if (str == "Pong")
            {
                stopWatch.Stop();
                recordPing?.Invoke(stopWatch.ElapsedMilliseconds);

                SendPing();
            }
        }

        public void OnError(IClient client, Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void SendPing()
        {
            if (tokenSource.IsCancellationRequested)
            {
                return;
            }

            var pongPacket = Encoding.Default.GetBytes("Ping");
            var sendPacket = new byte[pongPacket.Length + 4];

            Buffer.BlockCopy(BitConverter.GetBytes(4), 0, sendPacket, 0, 4);
            Buffer.BlockCopy(pongPacket, 0, sendPacket, 4, pongPacket.Length);

            client.SendAsync(sendPacket);
        
            stopWatch.Reset();
            stopWatch.Start();
        }
    }

    internal class Program
    {
        static CancellationTokenSource cancellationToken = new CancellationTokenSource();

        // index / ping
        static Dictionary<int, long> pingByClientIdx = new Dictionary<int, long>();

        static async Task Main(string[] args)
        {
            var lst = new List<MyClient>();
            var tasks = new List<Task>();
         
            for (int index = 0; index < 10000; ++index)
            {
                var client = new MyClient(index, (ping)=>
                {
                    pingByClientIdx[index] = ping;
                }, cancellationToken);

                lst.Add(client);
                await client.StartAsync();
            }

            //await Task.WhenAll(tasks);

            Console.WriteLine("Start Send ? (Input)");
            Console.ReadKey();

            Console.WriteLine("Start Send!");

            foreach (var clnt in lst)
            {
                clnt.SendPing();
            }

            var recordPing = RecordPing();

            Console.ReadKey();

            cancellationToken?.Cancel();

            tasks.Clear();
            foreach (var clnt in lst)
            {
                await clnt.StopAsync();
                //tasks.Add(clnt.StopAsync());
            }

            await recordPing;
        }

        static async Task RecordPing()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);

                long ping = 0;

                foreach (var pingPair in pingByClientIdx)
                {
                    ping += pingPair.Value;
                }

                if (ping == 0)
                {
                    continue;
                }

                Console.WriteLine(ping / pingByClientIdx.Count);
            }
        }
    }
}

