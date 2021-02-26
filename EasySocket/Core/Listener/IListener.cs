using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace EasySocket.Core.Listener
{
    public delegate void ListenerAcceptHandler(IListener listener, Socket socket);
    public delegate void ListenerErrorHandler(IListener listener, Socket socket);

    public interface IListener
    {
        ListenerConfig config { get; }

        event ListenerAcceptHandler accepted;
        event ListenerErrorHandler error;

        void Start(ListenerConfig config);
        void Close();
    }
}