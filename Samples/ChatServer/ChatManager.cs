using EasySocket.Server;
using NLog;
using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ChatManager : ISessionBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly ChatServer _server;
        private readonly ChatRoomManager _roomManager;
        private readonly ConcurrentDictionary<string, ChatSession> _chatSessions;
        private readonly ConcurrentDictionary<string, string> _nameToIdMap;

        public ChatRoomManager roomManager { get => _roomManager; }

        public ChatManager(ChatServer server)
        {
            _server = server;
            _roomManager = new ChatRoomManager(this);
            _chatSessions = new ConcurrentDictionary<string, ChatSession>();
            _nameToIdMap = new ConcurrentDictionary<string, string>();
        }

        public bool TryPreemptName(string id, string name)
        {
            return _nameToIdMap.TryAdd(name, id);
        }

        public void ReleaseName(string name)
        {
            _nameToIdMap.TryRemove(name, out _);
        }

        public void Init()
        {
            _chatSessions.Clear();
        }

        public async ValueTask OnReceivedAsync(ISession session, object packet)
        {
            if (session is null || packet is null)
            {
                return;
            }

            JSONNode json = packet as JSONNode;

            if (_chatSessions.TryGetValue(session.id, out var chatSession))
            {
                await chatSession.HandleMessageAsync(json);
            }
        }

        public async ValueTask OnStartAfterAsync(ISession session)
        {
            _logger.Debug($"Session - OnStartAfter id={session.id}");
        }

        public async ValueTask OnStartBeforeAsync(ISession session)
        {
            _logger.Debug($"Session - OnStartBefore id={session.id}");
        }

        public async ValueTask OnStoppedAsync(ISession session)
        {
            _logger.Debug($"Session - OnStopped id={session.id}");
        }

        public void OnError(ISession session, Exception e)
        {
            _logger.Error(e, $"Session - OnError id={session.id}");
        }

    }
}
