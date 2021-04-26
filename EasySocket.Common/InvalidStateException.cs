using System;

namespace EasySocket.Common
{
    public class InvalidObjectStateException : Exception
    {
        public InvalidObjectStateException(string objectName, string stateName)
            : base($"The \'{objectName}\' is in an invalid state : ObjectState is \'{stateName}\'")
        {

        }
    }
}