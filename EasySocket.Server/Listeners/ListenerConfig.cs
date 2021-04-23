using EasySocket.Common;

namespace EasySocket.Server.Listeners
{
    public readonly struct ListenerConfig
    {
        public string ip { get; }
        public int port { get; }
        public int backlog { get; }

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