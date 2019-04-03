using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnhancedStreamChat.Images
{
    /// <summary>
    /// An enum containing all the different types of supported emotes/images
    /// </summary>
    public enum ImageType
    {
        /// <summary>
        /// No image type
        /// </summary>
        None,
        /// <summary>
        /// Twitch emote
        /// </summary>
        Twitch,
        /// <summary>
        /// BetterTwitchTV emote
        /// </summary>
        BTTV,
        /// <summary>
        /// BetterTwitchTV animated emote
        /// </summary>
        BTTV_Animated,
        /// <summary>
        /// FrankerFaceZ emote
        /// </summary>
        FFZ,
        /// <summary>
        /// Twitch badge
        /// </summary>
        Badge,
        /// <summary>
        /// Any emoji
        /// </summary>
        Emoji,
        /// <summary>
        /// Twitch cheermote
        /// </summary>
        Cheermote
    };

    /// <summary>
    /// An interface to get the name associated with an ImageType
    /// </summary>
    public class ImageTypeNames
    {
        private static readonly string[] Names = new string[] { "None", "Twitch", "BetterTwitchTV", "BetterTwitchTV", "FrankerFaceZ", "Badges", "Emojis", "Cheermotes" };

        /// <summary>
        /// Retrieves an ImageTypeName for the provided ImageType
        /// </summary>
        /// <param name="type">The ImageType to retrieve the name of.</param>
        /// <returns></returns>
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
}
