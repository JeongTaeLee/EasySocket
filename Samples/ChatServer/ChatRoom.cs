using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ChatRoom
    {
        private string _name;
        private int _maxUser;
        private string _password;

        private ConcurrentDictionary<string, ChatSession> _users;

        public ChatRoom(string name, int maxUser, string password = null)
        {
            _name = name;
            _maxUser = maxUser;
            _password = password;

            _users = new ConcurrentDictionary<string, ChatSession>();
        }

        public async ValueTask BroadcastAsync(string type, JSONNode payload, Func<ChatSession, bool> predicate = null)
        {
            var query = _users.Values
                .Where(predicate)
                .Select(x => x.SendAsync(type, payload).AsTask());

            await Task.WhenAll(query);
        }

        public async ValueTask<bool> EnterUserAsync(ChatSession user, string password)
        {
            if (_password != null)
            {
                if (password == null || !string.Equals(_password, password))
                {
                    return false;
                }
            }

            if (!_users.TryAdd(user.id, user))
            {
                return false;
            }

            JSONNode payload = new JSONObject();
            payload["user_data"] = user.GetUserData();

            await BroadcastAsync(MessageTypes.BROADCAST_ENTER_USER, payload, x => !ReferenceEquals(x, user));

            return true;
        }

        public async ValueTask<bool> ExitUserAsync(ChatSession user)
        {
            if (!_users.TryRemove(user.id, out var outUser))
            {
                return false;
            }

            JSONNode payload = new JSONObject();
            payload["user_data"] = user.GetUserData();

            await BroadcastAsync(MessageTypes.BROADCAST_EXIT_USER, payload, x => !ReferenceEquals(x, user));

            return true;
        }
    }
}
