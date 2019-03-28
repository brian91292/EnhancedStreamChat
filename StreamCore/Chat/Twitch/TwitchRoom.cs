using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore.Chat
{
    public class TwitchRoom
    {
        public string name = "";
        public string roomId = "";
        public string lang = "";
        public bool emoteOnly;
        public bool followersOnly;
        public bool subsOnly;
        public bool r9k;
        public bool rituals;
        public bool slow;
        public TwitchRoom(string channel)
        {
            name = channel;
        }
    }
}
