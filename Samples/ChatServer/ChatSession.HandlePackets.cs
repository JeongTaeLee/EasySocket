using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public partial class ChatSession
    {

        private async ValueTask HandlePacket_LOGIN(long id, JSONNode payload)
        {
            string name = payload["name"];

            // 이름 선점
            if (_manager.TryPreemptName(_session.id, name))
            {
                var result = new JSONObject();
                result["result"] = true;

                await SendAsync(WrapPacket(MessageTypes.LOGIN_RESULT, id, result));
            }
            else
            {
                var result = new JSONObject();
                result["result"] = false;
                result["message"] = "The name already exists.";

                // 이미 누가 쓰는 이름이라면?
                await SendAsync(WrapPacket(MessageTypes.LOGIN_RESULT, id, result));
            }
        }

        private async ValueTask HandlePacket_ENTER_ROOM(long id, JSONNode payload)
        {
            string room_id = payload["room_id"];
        }

        private async ValueTask HandlePacket_EXIT_ROOM(long id, JSONNode payload)
        {

        }

        private async ValueTask HandlePacket_SEND_CHAT(long id, JSONNode payload)
        {

        }

        private async ValueTask HandlePacket_LOGOUT(long id, JSONNode payload)
        {

        }
    }
}
