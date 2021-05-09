using System;

namespace EasySocket.Common.Protocols
{
    public class ProtocolException : Exception
    {
        public ProtocolException(string message, Exception exception)
            : base(message, exception)
        {

        }

        public ProtocolException(string message)
            : base(message)
        {

        }
    }
}