using System.Collections;
namespace EasySocket.Server
{
    public interface ISessionContainer<TSession>
        where TSession : class, ISession
    {
        bool AddSession(string sessionId, TSession session);
        TSession RemoveSession(string sessionId);

        bool SetSession(string sessionId, TSession session);
        bool TryPreoccupancySessionId(out string sessionId);
        void CancelPreoccupancySessionId(string sessionId);

        TSession GetSession(string sessionId);
        IEnumerator GetSessionEnumerator();
    }
}