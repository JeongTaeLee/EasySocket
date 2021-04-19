using System;

namespace EasySocket.Common.Extensions
{
    public static class ExceptionExtensions
    {
        public static InvalidOperationException MemberNotSetIOE(string memberName, string methodName)
        {
            return new InvalidOperationException($"{memberName} is not set : Please call the \"{methodName}\" Method and set it up.");
        }
        
        public static InvalidOperationException InvalidObjectStateIOE<TEnum>(string objectName, TEnum state)
            where TEnum : Enum
        {
            return new InvalidOperationException($"The {objectName} has an invalid state. : {objectName }state is {state}");
        }

        public static InvalidOperationException CantStartObjectIOE<TEnum>(string objectName, TEnum state)
            where TEnum : Enum
        {
            return new InvalidOperationException($"Unable to start {objectName} : Invalid state. State({state})");
        }
        public static InvalidOperationException CantStopObjectIOE<TEnum>(string objectName, TEnum state)
            where TEnum : Enum
        {
            return new InvalidOperationException($"Unable to stop {objectName} : Invalid state. State({state})");
        }

        public static InvalidOperationException TerminatedObjectIOE(string objectName)
        {
            return new InvalidOperationException($"Unable to start {objectName} : This is a terminated {objectName}.");
        }
    }
}