using EasySocket.Server;
using NLog;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ChatServer
{
    public static class MessageTypes
    {
        // CLIENT -> SERVER
        public const string LOGIN = "LOGIN";
        public const string ENTER_ROOM = "ENTER_ROOM";
        public const string EXIT_ROOM = "EXIT_ROOM";
        public const string SEND_CHAT = "SEND_CHAT";
        public const string LOGOUT = "LOGOUT";

        // CLIENT -> SERVER -> CLIENT (RESPONSE)
        public const string LOGIN_RESULT = "LOGIN_RESULT";
        public const string ENTER_ROOM_RESULT = "ENTER_ROOM_RESULT";
        public const string EXIT_ROOM_RESULT = "EXIT_ROOM_RESULT";
        public const string SEND_CHAT_RESULT = "SEND_CHAT_RESULT";
        public const string LOGOUT_RESULT = "LOGOUT_RESULT";

        // BROADCAST
        public const string BROADCAST_ENTER_USER = "BROADCAST_ENTER_USER";
        public const string BROADCAST_EXIT_USER = "BROADCAST_EXIT_USER";
    }

    public partial class ChatSession
    {
        public delegate ValueTask PacketHandler(long id, JSONNode payload);

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private ISession _session;
        private readonly ImmutableDictionary<string, PacketHandler> _handlers;
        private ChatManager _manager;

        public string id { get => _session.id; }
        public string name { get; private set; }

        public ChatSession(ChatManager manager, ISession session)
        {
            this._session = session;
            this._manager = manager;
            this._handlers = CreateHandlers();
        }

        public JSONNode GetUserData()
        {
            JSONNode root = new JSONObject();
            root["name"] = name;
            root["id"] = id;
            root["status"] = "ONLINE";
            return root;
        }

        private ImmutableDictionary<string, PacketHandler> CreateHandlers()
        {
            return new Dictionary<string, PacketHandler>()
            {
                [MessageTypes.LOGIN] = HandlePacket_LOGIN,
                [MessageTypes.ENTER_ROOM] = HandlePacket_ENTER_ROOM,
                [MessageTypes.EXIT_ROOM] = HandlePacket_EXIT_ROOM,
                [MessageTypes.SEND_CHAT] = HandlePacket_SEND_CHAT,
                [MessageTypes.LOGOUT] = HandlePacket_LOGOUT,
            }.ToImmutableDictionary();
        }

        public async ValueTask HandleMessageAsync(JSONNode packet)
        {
            var payload = UnwrapPacket(packet, out string type, out long id);
            if (_handlers.TryGetValue(type, out var handler))
            {
                await handler.Invoke(id, payload);
            }
            else
            {
                _logger.Warn($"ChatSession - Failed to parse message {packet}");
            }
        }

        public async ValueTask SendAsync(string type, JSONNode payload)
        {
            await _session.SendAsync(WrapPacket(type, -1, payload).ToByteArray());
        }

        private async ValueTask SendAsync(JSONNode packet)
        {
            await _session.SendAsync(packet.ToByteArray());
        }

        private JSONNode WrapPacket(string type, long id, JSONNode payload)
        {
            JSONNode root = new JSONObject();
            root["type"] = type;
            root["id"] = id;
            root["payload"] = payload;

            return root;
        }

        private JSONNode UnwrapPacket(JSONNode packet, out string type, out long id)
        {
            type = packet["type"];
            id = packet["id"].AsLong;
            return packet["payload"];
        }
    }
}
