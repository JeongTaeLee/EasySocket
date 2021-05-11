using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ChatRoomManager
    {
        private readonly ChatManager _manager;

        private readonly ConcurrentDictionary<string, ChatRoom> _rooms;

        public ChatRoomManager(ChatManager manager)
        {
            _manager = manager;
            _rooms = new ConcurrentDictionary<string, ChatRoom>();
        }

        public bool CreateRoom(ChatSession creator, string name, string password = null)
        {
            if (_rooms.TryAdd(name, new ChatRoom(name, 20, password)))            
            {
                return true;
            }

            return false;
        }

        public async ValueTask<bool> CreateAndJoinRoomAsync(ChatSession creator, string name, string password = null)
        {
            if (!CreateRoom(creator, name, password))
            {
                return false;
            }

            return await JoinRoom(creator, name, password);
        }

        public async ValueTask<bool> JoinRoom(ChatSession user, string name, string password = null)
        {
            if (_rooms.TryGetValue(name, out var room))
            {
                return await room.EnterUserAsync(user, password);
            }

            return false;
        }
    }
}
