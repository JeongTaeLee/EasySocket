namespace EasySocket.Server.Listeners
{
    public class ListenerConfig
    {
        public readonly string ip;
        public readonly int port;
        public readonly int backlog;

        public ListenerConfig(string ip, int port, int backlog)
        {
            this.ip = ip;
            this.port = port;
            this.backlog = backlog;
        }

        public override string ToString()
        {
            return $"Ip({ip}) Port({port}) Backlog({backlog})";
        }
    }
}