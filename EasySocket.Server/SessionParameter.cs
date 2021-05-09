using System;
using EasySocket.Common.Logging;
using EasySocket.Common.Protocols;

namespace EasySocket.Server
{
    public class SessionParameter<TSession>
    {
        public readonly string sessionId;
        public readonly IMsgFilter msgFilter;
        public readonly ILogger logger;
        public readonly SocketSessionStopHandler<TSession> onStop;

        private SessionParameter() { }

        public SessionParameter(string ssnId, IMsgFilter msgFltr, SocketSessionStopHandler<TSession> onSt, ILogger lgr)
        {
            if (string.IsNullOrEmpty(ssnId))
            {
                throw new ArgumentNullException(nameof(ssnId));
            }

            sessionId = ssnId;
            msgFilter = msgFltr ?? throw new ArgumentNullException(nameof(msgFltr));
            onStop = onSt ?? throw new ArgumentNullException(nameof(onSt));
            logger = lgr ?? throw new ArgumentNullException(nameof(lgr));
        }
    }
}