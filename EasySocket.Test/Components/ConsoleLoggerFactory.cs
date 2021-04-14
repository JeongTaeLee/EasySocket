using System;
using EasySocket.Common.Logging;

namespace EasySocket.Test.Components
{
    public class ConsoleLoggerFactory : ILoggerFactory
    {
        public ILogger GetLogger(string name)
        {
            return new ConsoleLogger();
        }

        public ILogger GetLogger(Type type)
        {
            return new ConsoleLogger();
        }
    }
}