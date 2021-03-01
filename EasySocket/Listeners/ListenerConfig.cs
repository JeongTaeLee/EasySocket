namespace EasySocket.Listeners
{
    public class ListenerConfig
    {
        public readonly string ip;
        public readonly int port;
        public readonly int backlog;
        public readonly bool listenerNoDelay;

        public ListenerConfig(string ip, int port, int backlog, bool listenerNoDelay)
        {
            this.ip = ip;
            this.port = port;
            this.backlog = backlog;
            this.listenerNoDelay = listenerNoDelay;
        }
    }
}