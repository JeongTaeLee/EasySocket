using System;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Threading;

namespace EasySocket.Server
{
    public class TcpStreamPipeSocketSession : PipeSocketSession<TcpStreamPipeSocketSession>
    {
        private NetworkStream _networkStream = null;

        protected override ValueTask StartPipe(out PipeWriter writer, out PipeReader reader)
        {
            _networkStream = new NetworkStream(socket);

            writer = null;
            reader = PipeReader.Create(_networkStream);

            return new ValueTask();
        }

        protected override ValueTask StopPipe()
        {
            _networkStream?.Close();
            _networkStream = null;

            return new ValueTask();
        }

        protected override async Task ReadAsync(PipeReader reader)
        {
            try
            {
                while (true)
                {
                    var result = await pipeReader.ReadAsync();
                    var buffer = result.Buffer;

                    var readLength = 0L;

                    try
                    {
                        if (0 < buffer.Length)
                        {
                            readLength = ProcessReceive(buffer);
                        }

                        if (result.IsCanceled)
                        {
                            readLength = buffer.Length;
                            break;
                        }

                        if (result.IsCompleted)
                        {
                            readLength = buffer.Length;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        readLength = buffer.Length;
                        ProcessError(ex);

                        break;
                    }
                    finally
                    {
                        pipeReader.AdvanceTo(buffer.GetPosition(readLength));
                    }
                }
            }
            catch (SocketException ex)
            {
                // TODO : Exception
                ProcessError(ex);
            }
            catch (Exception ex)
            {
                // TODO : Exception
                ProcessError(ex);
            }
            finally
            {
                await pipeReader.CompleteAsync();
            }
        }

        public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry)
        {
            return await socket.SendAsync(mmry, SocketFlags.None);
        }
    }
}