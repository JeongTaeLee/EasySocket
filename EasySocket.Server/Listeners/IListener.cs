using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EasySocket.Server.Listeners
{
    public delegate void ListenerAcceptHandler(IListener listener, Socket socket);
    public delegate void ListenerErrorHandler(IListener listener, Exception ex);

    public interface IListener
    {
        ListenerAcceptHandler onAccept { get; set; }
        ListenerErrorHandler onError { get; set; }

        Task StartAsync(ListenerConfig cnfg);
        Task StopAsync();
    }
}