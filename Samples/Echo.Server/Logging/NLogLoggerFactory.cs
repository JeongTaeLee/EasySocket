using System;
using EasySocket.Common.Logging;

namespace Echo.Server.Logging
{
    public class NLogLoggerFactory : LogFactoryBase
    {
        public NLogLoggerFactory(string config)
            : base(config)
        {
            if (!IsSharedConfig)
            {
                NLog.Config.XmlLoggingConfiguration.SetCandidateConfigFilePaths(new[] { ConfigFile });
            }
            else
            {
            }
        }

        public override ILogger GetLogger(string name)
        {
            return new NLogLogger(NLog.LogManager.GetLogger(name));
        }

        public override ILogger GetLogger(Type type)
        {
            return new NLogLogger(NLog.LogManager.GetLogger(type.Name));
        }
    }
}
