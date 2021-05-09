using System;

namespace EasySocket.Common
{
    public class MemberNotSetException : Exception
    {
        public MemberNotSetException(string memberName)
            : base($"{memberName} is not set")
        {
        }

        public MemberNotSetException(string memberName, string setterName)
            : base($"\'{memberName}\' is not set : Please set it up using the \'{setterName}\'")
        {
        }
    }
}