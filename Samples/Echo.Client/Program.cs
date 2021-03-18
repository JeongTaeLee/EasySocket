using System;
using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace Echo.Client
{
    class Program
    {
        // P/Invoke:
        private enum StdHandle { Stdin = -10, Stdout = -11, Stderr = -12 };
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(StdHandle std);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hdl);

        static CancellationTokenSource cancelationToken = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine("Start Connect");

            await socket.ConnectAsync("127.0.0.1", 9199);

            Console.WriteLine("Connected");

            var receiveTask = ProcessReceive(socket);
            var processReceive = ProcessSend(socket);

            await receiveTask;

            //socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket.Close();

            Console.WriteLine("Close");

            await receiveTask;
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

