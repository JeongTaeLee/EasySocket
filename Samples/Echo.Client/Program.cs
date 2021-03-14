using System;
using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Threading;

namespace Echo.Client
{
    class Program
    {
        static CancellationTokenSource cancelationToken = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine("Start Connect");

            await socket.ConnectAsync("127.0.0.1", 9199);

            Console.WriteLine("Connected");

            var receiveTask = ProcessReceive(socket);

            while (true)
            {
                var inputStr = Console.ReadLine();
                if (inputStr == "Close")
                {
                    break;
                }

                var sendByte = Encoding.Default.GetBytes(inputStr);
                
                var sendLength = await socket.SendAsync(sendByte, SocketFlags.None);
                Console.WriteLine($"Sended({sendLength})");
            }

            Console.WriteLine("Close");

            cancelationToken.Cancel();
            await receiveTask;
        }

        static async Task ProcessReceive(Socket socket)
        {
            var networkStream = new NetworkStream(socket);
            var pipeReader = PipeReader.Create(networkStream);

            while (cancelationToken.IsCancellationRequested)
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

                    readLength = buffer.Length;

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

            networkStream.Close();

            // while (true)
            // {
            //     await socket.ReceiveAsync()
            // }
        }
    }
}

