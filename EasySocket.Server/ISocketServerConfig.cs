namespace EasySocket.Server
{
    public interface ISocketServerConfig
    {
        int maxConnection { get; }

        int recvBufferSize { get; }
        int sendBufferSize { get; }

        int recvTimeout { get; }
        int sendTimeout { get; }

        bool noDelay { get; 
        }
        ISocketServerConfig DeepClone();
    }
}