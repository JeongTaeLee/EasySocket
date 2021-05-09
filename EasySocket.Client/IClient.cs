﻿using System;
using System.Threading.Tasks;

namespace EasySocket.Client
{
    public enum ClientState
    {
        None = 0,
        Starting,
        Running,
        Stopping,
        Stopped,
    }

    public interface IClient
    {
        ClientState state { get; }
        IClientBehaviour behaviour { get; }

        Task StopAsync();
        Task<int> SendAsync(byte[] buffer);
        Task<int> SendAsync(ArraySegment<byte> segement);
    }
}