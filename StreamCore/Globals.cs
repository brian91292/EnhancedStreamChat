using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore
{
    public enum ImageType
    {
        None,
        Twitch,
        BTTV,
        BTTV_Animated,
        FFZ,
        Badge,
        Emoji,
        Cheermote
    };

    public class ImageTypeNames
    {
        private static readonly string[] Names = new string[] { "None", "Twitch", "BetterTwitchTV", "BetterTwitchTV", "FrankerFaceZ", "Badges", "Emojis", "Cheermotes" };

        public static string Get(ImageType type)
        {
            return Names[(int)type];
        }
    };

    public class ImageInfo
    {
        public char swapChar;
        public string textureIndex;
        public ImageType imageType;
    }

    public class BadgeInfo : ImageInfo
    {
    };

    public class EmoteInfo : ImageInfo
    {
        public string swapString;
        public bool isEmoji;
    };

    public class Globals
    {
        public static readonly string DataPath = Path.Combine(Environment.CurrentDirectory, "UserData", Plugin.ModuleName);

        public static bool IsApplicationExiting = false;
        public static bool IsAtMainMenu = true;
    }
}
