using System;
using System.Collections;
using System.Collections.Concurrent;

namespace EasySocket.Server
{
    public class GUIDSessionContainer<TSession> : ISessionContainer<TSession>
        where TSession : class, ISession
    {
        private const int _sessionIdAttemptMaxCount = 100;

        private ConcurrentDictionary<string, TSession> _sessions = new ConcurrentDictionary<string, TSession>();

        public bool AddSession(string sessionId, TSession session)
        {
            return _sessions.TryAdd(sessionId, session);
        }

        public TSession GetSession(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public TSession RemoveSession(string sessionId)
        {
            _sessions.TryRemove(sessionId, out var session);
            return session;
        }

        public bool SetSession(string sessionId, TSession session)
        {
            if (!_sessions.ContainsKey(sessionId))
            {
                return false;
            }

            _sessions[sessionId] = session;

            return true;
        }
        public bool TryPreoccupancySessionId(out string sessionId)
        {
            int attemptCount = 0;

            var returnSessionId = string.Empty;

            do
            {
                ++attemptCount;
            
                var tempSessionId = Guid.NewGuid().ToString();
                
                if (_sessions.TryAdd(tempSessionId, null))
                {
                    returnSessionId = tempSessionId;
                    break;
                }
            }
            while (attemptCount < _sessionIdAttemptMaxCount);

            sessionId = returnSessionId;

            return !string.IsNullOrEmpty(sessionId);
        }

        public void CancelPreoccupancySessionId(string sessionId)
        {
            _sessions.TryRemove(sessionId, out var session);
        }

        public IEnumerator GetSessionEnumerator()
        {
            return _sessions.GetEnumerator();
        }
    }
}