
namespace EasySocket.Logging
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

        public override ILogger GetLogger<TType>()
        {
            return new NLogLogger(NLog.LogManager.GetCurrentClassLogger(typeof(TType)));
        }
    }
}
