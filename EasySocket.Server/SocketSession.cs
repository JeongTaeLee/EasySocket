using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EasySocket.Common.Extensions;
using EasySocket.Common.Logging;

namespace EasySocket.Server
{
    public abstract class SocketSession<TSession, TPacket> : BaseSession<TSession, TPacket>
        where TSession : BaseSession<TSession, TPacket>
    {
        private SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        protected Socket socket { get; private set; } = null;

        protected override void InternalInitialize()
        {
            base.InternalInitialize();

            if (socket == null)
            {
                throw ExceptionExtensions.MemberNotSetIOE("Socket", "SetSocket");
            }
        }

        public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> mmry)
        {
            if (state != SessionState.Running)
            {
                throw new InvalidOperationException($"The session has an invalid state. : Session state is {state}");
            }
            
            try
            {
                await _sendLock.WaitAsync();

                return await ProcessSend(mmry);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public TSession SetSocket(Socket sck)
        {
            socket = sck ?? throw new ArgumentNullException(nameof(sck));
            return this as TSession;
        }

        protected abstract ValueTask<int> ProcessSend(ReadOnlyMemory<byte> mmry);
    }
}