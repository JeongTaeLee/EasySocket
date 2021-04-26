using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public class TcpStreamPipeSocketClient : PipeSocketClient<TcpStreamPipeSocketClient>
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

                // TODO : Exception
                ProcessError(ex);    
            }
            catch (SocketException ex)
            {                    // ignore Exception
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

        public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> sendMemory)
        {
            return await socket.SendAsync(sendMemory, SocketFlags.None);
        }

        protected override Socket CreateSocket(SocketClientConfig sckCnfg)
        {
           return new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                SendBufferSize = sckCnfg.sendBufferSize,
                ReceiveBufferSize = sckCnfg.receiveBufferSize
            };
        }
    }
}