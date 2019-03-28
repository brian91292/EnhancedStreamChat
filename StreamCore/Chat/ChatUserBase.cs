using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore.Chat
{
    public class ChatUserBase
    {
        public string id = "";
        public string username = "";
        public string displayName = "";
        public bool isBroadcaster;
        public bool isMod;
    }
}
