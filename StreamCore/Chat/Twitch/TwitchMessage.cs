using StreamCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamCore.Chat
{
    public class TwitchMessage : ChatMessageBase
    {
        public string hostString = "";
        public string messageType = "";
        public string channelName = "";
        public string roomId = "";
        public string emotes = "";
        public int bits;
        public TwitchUser user = new TwitchUser();

        /// <summary>
        /// All the tags associated with the current TwitchMessage. Tag = match.Groups["Tag"].Value, Value = match.Groups["Value"].Value
        /// </summary>
        public MatchCollection tags;
    }
}
