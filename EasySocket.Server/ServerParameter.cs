using EasySocket.Common.Logging;
using EasySocket.Common.Protocols.Factories;

namespace EasySocket.Server
{
    public class ServerParameter
    {
        public readonly ILoggerFactory loggerFactory;
        public readonly IMsgFilterFactory msgFilterFactory;
        
    }
}