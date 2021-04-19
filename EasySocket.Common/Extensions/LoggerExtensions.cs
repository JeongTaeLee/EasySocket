using System;
using EasySocket.Common.Logging;

namespace EasySocket.Common.Extensions
{
    public static class LoggerExtensions
    {
        public static void InvalidObjectStateError<TEnum>(this ILogger logger, string objectName, TEnum state)
            where TEnum : Enum
        {
            logger?.Error($"The {objectName} has an invalid state. : {objectName }state is {state}");
        }


        public static void MemberNotSetUseMethodWarn(this ILogger logger, string memberName, string methodName)
        {
            logger?.Warn($"{memberName} is not set : Please call the \"{methodName}\" Method and set it up.");
        }

        public static void MemberNotSetUsePropWarn(this ILogger logger, string memberName, string propertyName)
        {
            logger?.Warn($"{memberName} is not set : Please call the \"{propertyName}\" property and set it up.");
        }

        public static void MemberNotSetUsePropWarn(this ILogger logger, string propertyName)
        {
            logger?.Warn($"{propertyName} is not set : Please call the \"{propertyName}\" property and set it up.");
        }


    }
}