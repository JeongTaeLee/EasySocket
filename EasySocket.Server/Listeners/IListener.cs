using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EasySocket.Common.Logging;

namespace EasySocket.Server.Listeners
{
    public delegate ValueTask ListenerAcceptHandler(IListener listener, Socket socket);
    public delegate void ListenerErrorHandler(IListener listener, Exception ex);

    public interface IListener
    {
        ListenerAcceptHandler onAccept { get; set; }
        ListenerErrorHandler onError { get; set; }

        Task StartAsync(ListenerConfig cnfg, ILogger logger);
        Task StopAsync();
    }
}