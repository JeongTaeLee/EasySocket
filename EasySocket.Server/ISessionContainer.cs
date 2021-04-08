using System.Collections;
namespace EasySocket.Server
{
    public interface ISessionContainer<TSession>
        where TSession : class, ISession
    {
        public int count { get; }

        bool AddSession(string sessionId, TSession session);
        TSession RemoveSession(string sessionId);
        bool SetSession(string sessionId, TSession session);

        bool TryPreoccupancySessionId(out string sessionId);

        TSession GetSession(string sessionId);
    }
}