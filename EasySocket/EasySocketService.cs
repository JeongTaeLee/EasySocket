using System;
using System.Threading.Tasks;
using EasySocket.Servers;
using EasySocket.Sessions;
using EasySocket.Common.Logging;

namespace EasySocket
{
    public sealed class EasySocketService
    {
        public ILoggerFactory loggerFactroy { get; private set; } = null;
        public ILogger logger { get; private set; } = null;

        public Func<ISocketServer> serverGenerator { get; private set; } = null;
        public Action<ISocketServer> serverConfigrator { get; private set; } = null;
        public Action<ISocketSession> sessionConfigrator { get; private set; } = null;
        public ISocketServer server { get; private set; } = null;

        public EasySocketService()
        {
        }

        public void Start()
        {
            if (loggerFactroy == null)
            {
                throw new InvalidOperationException("LoggerFactroy not set : Please call the \"SetLoggerFactroy\" Method and set it up.");
            }

            logger = loggerFactroy.GetLogger(GetType());
            if (logger == null)
            {
                throw new ArgumentNullException("Unable to create logger.");
            }

            if (serverGenerator == null)
            {
                throw new InvalidOperationException("SocketServer not set : Please call the \"SetSocketServer\" Method and set it up.");
            }

            if (serverConfigrator == null)
            {
                throw new InvalidOperationException("SocketServer Configrator not set : Please call the \"SetSocketServerConfigrator\" Method and set it up");
            }

            if (sessionConfigrator == null)
            {
                throw new InvalidOperationException("SocketSession Configrator not set : Please call the \"SetSocketSessionConfigrator\" Method and set it up");
            }

            server = serverGenerator.Invoke();
            if (server == null)
            {
                throw new InvalidOperationException("Server generator returned null");
            }

            serverConfigrator.Invoke(server);

            server.Start(this);
        }

        public void Stop()
        {
            if (server == null)
            {
                throw new InvalidOperationException("Server is not set : The server did not start.");
            }

            server.Stop();
        }

        public async Task StopAsync()
        {
            if (server == null)
            {
                throw new InvalidOperationException("Server is not set : The server did not start.");
            }

            await server.StopAsync();
        }

        /// <summary>
        /// 해당 <see cref="EasySocketService"/>에서 사용하는 <see cref="ILoggerFactory"/>를 설정합니다.
        /// </summary>
        public EasySocketService SetLoggerFactroy(ILoggerFactory lgrFctry)
        {
            if (lgrFctry == null)
            {
                throw new ArgumentNullException(nameof(lgrFctry));
            }

            loggerFactroy = lgrFctry;

            return this;
        }

        /// <summary>
        /// 해당 <see cref="EasySocketService"/>에서 실행할 <see cref="ISocketServer"/>를 설정합니다.
        /// 설정된 <see cref="ISocketServer"/>는 <see cref="EasySocketService.Start"/>에서 생성됩니다.
        /// </summary>
        public EasySocketService SetSocketServer<TSocketServer>()
            where TSocketServer : class, ISocketServer, new()
        {
            serverGenerator = () => { return new TSocketServer(); };
            return this;
        }

        /// <summary>
        /// 해당 <see cref="EasySocketService"/>에서 실행할 <see cref="ISocketServer"/>를 구성하는 메서드를 설정합니다.
        /// 설정된 메서드는 <see cref="ISocketServer"/>가 생성된 후 단 한번 호출됩니다.
        /// </summary>
        public EasySocketService SetSocketServerConfigrator(Action<ISocketServer> srvCnfgr)
        {
            if (srvCnfgr == null)
            {
                throw new ArgumentNullException(nameof(srvCnfgr));
            }

            serverConfigrator = srvCnfgr;

            return this;
        }

        /// <summary>
        /// <see cref="ISocketServer"/>에서 새로운 세션이 연결되고 <see cref="ISocketSession"/>가 생성된 후 호출됩니다.
        /// 설정된 메서드는 새로운 <see cref="ISocketSession"/>가 생성될 때 각 한번 호출됩니다.
        /// </summary>
        public EasySocketService SetSocketSessionConfigrator(Action<ISocketSession> seinCnfgr)
        {
            if (seinCnfgr == null)
            {
                throw new ArgumentNullException(nameof(seinCnfgr));
            }

            sessionConfigrator = seinCnfgr;

            return this;
        }
    }
}