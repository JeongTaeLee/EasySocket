using System;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

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
                            var readedSeq = await ProcessReceive(buffer);
                            readLength = buffer.Length - readedSeq.Length;
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
                        pipeReader.AdvanceTo(buffer.GetPosition(readLength), buffer.GetPosition(buffer.Length));
                    }
                }
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException)
                {
                    var socketEx = ex.InnerException as SocketException;

                    // ignore Exception
                    if (socketEx.ErrorCode == 89)
                    {
                        return;
                    }   
                }
            }
            catch (SocketException ex)
            {
                // ignore Exception
                if (ex.ErrorCode == 89)
                {
                    return;
                }   

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

        public override int Send(byte[] buffer)
        {
            return socket.Send(buffer);
        }

        public override int Send(ArraySegment<byte> segment)
        {
            return socket.Send(segment);
        }

        public override int Send(byte[] buffer, int offset, int count)
        {
            return socket.Send(buffer, offset, count, SocketFlags.None);
        }

        public override ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry)
        {
            return socket.SendAsync(mmry, SocketFlags.None);
        }
    }
}