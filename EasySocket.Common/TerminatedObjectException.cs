using System;

namespace EasySocket.Common
{
    public class TerminatedObjectException : Exception
    {
        public TerminatedObjectException(string objectName)
            : base($"Terminated {objectName} cannot be reused.")
        {
        }
    }
}