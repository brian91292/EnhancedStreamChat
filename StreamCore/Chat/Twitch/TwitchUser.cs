using StreamCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore.Chat
{
    public class TwitchUser : ChatUserBase
    {
        public string color = "";
        public string badges = "";
        public bool isSub;
        public bool isTurbo;
        public bool isVip;
        public TwitchUser(string username = "")
        {
            displayName = username;
        }
        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add("displayName", new JSONString(displayName));
            obj.Add("id", new JSONString(id));
            obj.Add("color", new JSONString(color));
            obj.Add("badges", new JSONString(badges));
            obj.Add("isBroadcaster", new JSONBool(isBroadcaster));
            obj.Add("isMod", new JSONBool(isMod));
            obj.Add("isSub", new JSONBool(isSub));
            obj.Add("isTurbo", new JSONBool(isTurbo));
            obj.Add("isVip", new JSONBool(isVip));
            return obj;
        }

        public void FromJson(JSONObject obj)
        {
            displayName = obj["displayName"].Value;
            id = obj["id"].Value;
            color = obj["color"].Value;
            badges = obj["badges"].Value;
            isBroadcaster = obj["isBroadcaster"].AsBool;
            isMod = obj["isMod"].AsBool;
            isSub = obj["isSub"].AsBool;
            isTurbo = obj["isTurbo"].AsBool;
            isVip = obj["isVip"].AsBool;
        }
    }
}
