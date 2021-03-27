using System;

namespace EasySocket.Common.Extensions
{
    public static class ExceptionExtensions
    {
        public static InvalidOperationException MemberNotSetIOE(string memberName, string methodName)
        {
            return new InvalidOperationException($"{memberName} is not set : Please call the \"{methodName}\" Method and set it up.");
        }
    }
}