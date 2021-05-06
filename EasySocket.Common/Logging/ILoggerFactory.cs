using System;

namespace EasySocket.Common.Logging
{
    public interface ILoggerFactory
    {
        ILogger GetLogger(string name);
        ILogger GetLogger(Type type);
    }
}
