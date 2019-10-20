using EnhancedStreamChat.Textures;
using EnhancedStreamChat.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        Cheermote,
        /// <summary>
        /// YouTube profile image
        /// </summary>
        YouTube_Profile
    };

    /// <summary>
    /// An interface to get the name associated with an ImageType
    /// </summary>
    public class ImageTypeNames
    {
        private static readonly string[] Names = new string[] { "None", "Twitch", "BetterTwitchTV", "BetterTwitchTV", "FrankerFaceZ", "Badges", "Emojis", "Cheermotes", "YouTube_Profile" };

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
        private CachedSpriteData _cachedSprite = null;
        public CachedSpriteData cachedSprite
        {
            get
            {
                if(_cachedSprite == null || (_cachedSprite.sprite == null && _cachedSprite.animInfo == null))
                    ImageDownloader.CachedTextures.TryGetValue(textureIndex, out _cachedSprite);
                return _cachedSprite;
            }
        }
        public Task<CachedSpriteData> GetCachedSprite()
        {
            return Task.Run(() => {
                var sprite = cachedSprite;
                if (sprite == null)
                {
                    while (!ImageDownloader.CachedTextures.TryGetValue(textureIndex, out _cachedSprite))
                    {
                        Thread.Sleep(0);
                    }
                    while(_cachedSprite.sprite == null && _cachedSprite.animInfo == null)
                    {
                        Thread.Sleep(0);
                    }
                }
                return _cachedSprite;
            });
        }
        //private string _spacingString = "\u200A";
        //public string spacingString
        //{
        //    get
        //    {
        //        var sprite = cachedSprite;
        //        if (sprite != null)
        //        {
        //            var count = (int)Math.Floor(sprite.width * 0.064f / Drawing.imageSpacingWidth);
        //            _spacingString = new StringBuilder(Drawing.spacingChar.Length * count).Insert(0, Drawing.spacingChar, count).ToString();
        //        }
        //        return spacingString;
        //    }
        //}
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
