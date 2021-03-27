using EasySocket.Common.Logging;

namespace EasySocket.Common.Extensions
{
    public static class LoggerExtensions
    {
        public static void MemberNotSetWarn(this ILogger logger, string memberName, string methodName)
        {
            logger?.Warn($"{memberName} is not set : Please call the \"{methodName}\" Method and set it up.");
        }
    }
}