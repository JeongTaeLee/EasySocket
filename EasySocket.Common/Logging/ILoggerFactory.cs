using System;

namespace EasySocket.Common.Logging
{
    public interface ILoggerFactory
    {
        public ILogger GetLogger(string name);
        public ILogger GetLogger(Type type);
    }
}
