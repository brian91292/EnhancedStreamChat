using EnhancedStreamChat.Images;
using StreamCore.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamCore.Chat;
using StreamCore.YouTube;

namespace EnhancedStreamChat.Chat
{
    public class ChatMessage
    {
        public string displayMsg = String.Empty;
        public string displayColor = "";
        public GenericChatMessage origMessage;
        public List<EmoteInfo> parsedEmotes = new List<EmoteInfo>();
        public List<BadgeInfo> parsedBadges = new List<BadgeInfo>();

        public ChatMessage(string msg, GenericChatMessage messageInfo)
        {
            this.displayMsg = msg;
            this.origMessage = messageInfo;
        }
    };
}
