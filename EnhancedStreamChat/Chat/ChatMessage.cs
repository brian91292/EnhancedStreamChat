using EnhancedStreamChat.Images;
using StreamCore.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnhancedStreamChat.Chat
{
    public class ChatMessage
    {
        public string msg = String.Empty;
        public TwitchMessage twitchMessage = new TwitchMessage();
        public List<EmoteInfo> parsedEmotes = new List<EmoteInfo>();
        public List<BadgeInfo> parsedBadges = new List<BadgeInfo>();
        public bool isActionMessage = false;

        public ChatMessage(string msg, TwitchMessage messageInfo)
        {
            this.msg = msg;
            this.twitchMessage = messageInfo;
        }
    };
}
