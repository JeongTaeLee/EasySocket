using System;
using EasySocket.Common.Protocols.MsgInfos;

namespace EasySocket.Server
{
    public interface ISessionBehavior
    {
        void OnStarted(ISession ssn);
        void OnStopped(ISession ssn);
        void OnReceived(ISession ssn, IMsgInfo msgInfo);
        void OnError(ISession ssn, Exception ex);
    }
}