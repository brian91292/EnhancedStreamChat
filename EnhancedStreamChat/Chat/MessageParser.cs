using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using EnhancedStreamChat.Chat;
using System.Text.RegularExpressions;
using EnhancedStreamChat.UI;
using Random = System.Random;
using EnhancedStreamChat.Images;
using StreamCore;
using EnhancedStreamChat.Config;
using EnhancedStreamChat.Textures;
using StreamCore.Chat;
using StreamCore.YouTube;
using StreamCore.Twitch;
using System.Globalization;

namespace EnhancedStreamChat.Chat
{
    public static class StringBuilderExtensions
    {
        public static int IndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
        {
            int index;
            int length = value.Length;
            int maxSearchLength = (sb.Length - length) + 1;

            if (ignoreCase)
            {
                for (int i = startIndex; i < maxSearchLength; ++i)
                {
                    if (Char.ToLower(sb[i]) == Char.ToLower(value[0]))
                    {
                        index = 1;
                        while ((index < length) && (Char.ToLower(sb[i + index]) == Char.ToLower(value[index])))
                            ++index;

                        if (index == length)
                            return i;
                    }
                }

                return -1;
            }

            for (int i = startIndex; i < maxSearchLength; ++i)
            {
                if (sb[i] == value[0])
                {
                    index = 1;
                    while ((index < length) && (sb[i + index] == value[index]))
                        ++index;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf(this StringBuilder sb, char find, bool ignoreCase = false, int startIndex = -1, CultureInfo culture = null)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            if (startIndex == -1) startIndex = sb.Length - 1;
            if (startIndex < 0 || startIndex >= sb.Length) throw new ArgumentException("startIndex must be between 0 and sb.Lengh-1", nameof(sb));
            if (culture == null) culture = CultureInfo.InvariantCulture;

            int lastIndex = -1;
            if (ignoreCase) find = Char.ToUpper(find, culture);
            for (int i = startIndex; i >= 0; i--)
            {
                char c = ignoreCase ? Char.ToUpper(sb[i], culture) : (sb[i]);
                if (find == c)
                {
                    lastIndex = i;
                    break;
                }
            }
            return lastIndex;
        }
    }

    public static class TwitchMessageExtensions
    {
        private static readonly Regex _emoteRegex = new Regex(@"(?<EmoteIndex>[a-zA-Z0-9_-]+):(?<StartIndex>[0-9]+)-(?<EndIndex>[0-9]+)", RegexOptions.Compiled);
        private static readonly Regex _badgeRegex = new Regex(@"(?<BadgeName>[^,\/]+)\/(?<BadgeVersion>[^,;]+)", RegexOptions.Compiled);

        private static readonly Dictionary<string, ImageInfo> _imageInfoByID = new Dictionary<string, ImageInfo>();
        private static readonly Dictionary<string, ImageInfo> _imageInfoByName = new Dictionary<string, ImageInfo>();
        public static void ParseEmotes(this TwitchMessage message, ref char imgPlacementDesignator, ref StringBuilder messageBuilder, out bool isActionMessage, ChatMessage chatMessage)
        {
            _imageInfoByID.Clear();
            _imageInfoByName.Clear();

            // Action messages
            isActionMessage = chatMessage.displayMsg.StartsWith((char)0x1 + "ACTION");
            if (isActionMessage)
                messageBuilder = messageBuilder.Remove(messageBuilder.Length - 1, 1).Remove(0, 8);

            var emojilessMessage = EmojiUtilities.emojiRegex.Replace(chatMessage.displayMsg, " ");


            // Emotes
            var twitchEmotes = _emoteRegex.Matches(chatMessage.origMessage.Twitch.emotes);
            if (twitchEmotes.Count > 0)
            {
                foreach (Match e in twitchEmotes)
                {
                    string emoteIndex = $"T{e.Groups["EmoteIndex"].Value}";
                    if (!ImageDownloader.CachedTextures.ContainsKey(emoteIndex))
                        ImageDownloader.Instance.Queue(new TextureDownloadInfo(emoteIndex, ImageType.Twitch, chatMessage.origMessage.id));

                    if (!_imageInfoByID.TryGetValue(emoteIndex, out var emoteInfo))
                    {
                        int startIndex = Convert.ToInt32(e.Groups["StartIndex"].Value);
                        string emoteName = emojilessMessage.Substring(startIndex, Convert.ToInt32(e.Groups["EndIndex"].Value) - startIndex + 1);

                        Plugin.Log($"Emote: {emoteName}");

                        emoteInfo = new EmoteInfo()
                        {
                            swapChar = imgPlacementDesignator++,
                            swapString = emoteName,
                            textureIndex = emoteIndex,
                            imageType = ImageType.Twitch
                        };
                        _imageInfoByID[emoteIndex] = emoteInfo;
                        _imageInfoByName[emoteName] = emoteInfo;
                    }
                }
                Thread.Sleep(0);
            }

            // BTTV/FFZ/cheeremotes
            int lastSpaceIndex = 0;
            while (true)
            {
                bool done = false;
                int spaceIndex = messageBuilder.IndexOf(" ", lastSpaceIndex, false);
                if (spaceIndex == -1)
                    done = true;

                int startIndex = lastSpaceIndex;
                int endIndex = spaceIndex == -1 ? messageBuilder.Length - lastSpaceIndex : spaceIndex - lastSpaceIndex;
                string word = messageBuilder.ToString(lastSpaceIndex, endIndex);
                lastSpaceIndex = spaceIndex + 1;

                if (!_imageInfoByName.TryGetValue(word, out var imageInfo))
                {
                    string textureIndex = String.Empty;
                    ImageType imageType = ImageType.None;
                    string tmpTextureIndex = "";
                    if (ImageDownloader.BTTVEmoteIDs.TryGetValue(word, out tmpTextureIndex))
                    {
                        if (ChatConfig.Instance.ShowBTTVEmotes)
                        {
                            textureIndex = $"B{tmpTextureIndex}";
                            imageType = ImageType.BTTV;
                        }
                    }
                    else if (ImageDownloader.BTTVAnimatedEmoteIDs.TryGetValue(word, out tmpTextureIndex))
                    {
                        if (ChatConfig.Instance.ShowBTTVEmotes)
                        {
                            textureIndex = $"AB{tmpTextureIndex}";
                            imageType = ImageType.BTTV_Animated;
                        }
                    }
                    else if (ImageDownloader.FFZEmoteIDs.TryGetValue(word, out tmpTextureIndex))
                    {
                        if (ChatConfig.Instance.ShowFFZEmotes)
                        {
                            textureIndex = $"F{tmpTextureIndex}";
                            imageType = ImageType.FFZ;
                        }
                    }
                    else if (chatMessage.origMessage.Twitch.bits > 0 && EmojiUtilities.cheermoteRegex.IsMatch(word.ToLower()))
                    {
                        word = word.ToLower();
                        Match match = EmojiUtilities.cheermoteRegex.Match(word);
                        string prefix = match.Groups["Prefix"].Value;
                        if (ImageDownloader.TwitchCheermoteIDs.TryGetValue(prefix, out var cheermote))
                        {
                            int bits = Convert.ToInt32(match.Groups["Value"].Value);
                            string tier = cheermote.GetTier(bits);
                            textureIndex = $"{prefix}{tier}";
                            imageType = ImageType.Cheermote;
                        }
                    }

                    if (imageType != ImageType.None)
                    {
                        if (!ImageDownloader.CachedTextures.ContainsKey(textureIndex))
                            ImageDownloader.Instance.Queue(new TextureDownloadInfo(textureIndex, imageType, chatMessage.origMessage.id));

                        imageInfo = new EmoteInfo()
                        {
                            imageType = imageType,
                            swapChar = imgPlacementDesignator++,
                            swapString = word,
                            textureIndex = textureIndex
                        };
                        _imageInfoByID[textureIndex] = imageInfo;
                        _imageInfoByName[word] = imageInfo;
                    }
                }
                if (imageInfo != null)
                {
                    var emoteInfo = (EmoteInfo)imageInfo;
                    chatMessage.parsedEmotes[emoteInfo.textureIndex] = emoteInfo;

                    var cachedSprite = emoteInfo.GetCachedSprite();
                    string extraInfo = String.Empty;
                    if (emoteInfo.imageType == ImageType.Cheermote)
                    {
                        try
                        {
                            Match cheermote = EmojiUtilities.cheermoteRegex.Match(emoteInfo.swapString);
                            if (cheermote.Success)
                            {
                                string numBits = cheermote.Groups["Value"].Value;
                                string prefix = cheermote.Groups["Prefix"].Value.ToLower();
                                extraInfo = $"\u200A<color={ImageDownloader.TwitchCheermoteIDs[prefix].GetColor(Convert.ToInt32(numBits))}><size=4><b>{numBits}</b></size></color>\u200A";
                            }
                            else
                            {
                                Plugin.Log($"Cheermote regex failed for message {chatMessage.origMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log($"Failed to parse cheermote! {ex.ToString()}");
                        }
                    }
                    var replaceStr = $"\ufeff{Char.ConvertFromUtf32(emoteInfo.swapChar)}{emoteInfo.spacingString}{extraInfo}";
                    messageBuilder.Remove(startIndex, endIndex);
                    messageBuilder.Insert(startIndex, replaceStr);
                    lastSpaceIndex += (replaceStr.Length - emoteInfo.swapString.Length);
                }

                if (done)
                    break;

                Thread.Sleep(0);
            }
        }

        public static void ParseBadges(this TwitchMessage message, ref char imgPlacementDesignator, ref StringBuilder messageBuilder, ChatMessage chatMessage)
        {
            // Badges
            var badges = _badgeRegex.Matches(chatMessage.origMessage.user.Twitch.badges);
            if (badges.Count > 0)
            {
                foreach (Match badge in badges.Cast<Match>().Reverse())
                {
                    string badgeName = $"{badge.Groups["BadgeName"].Value}{badge.Groups["BadgeVersion"].Value}";
                    if (ImageDownloader.TwitchBadgeIDs.TryGetValue(badgeName, out var badgeIndex))
                    {
                        if (!ImageDownloader.CachedTextures.ContainsKey(badgeIndex))
                            ImageDownloader.Instance.Queue(new TextureDownloadInfo(badgeIndex, ImageType.Badge, chatMessage.origMessage.id));

                        if (!_imageInfoByID.TryGetValue(badgeIndex, out var imageInfo))
                        {
                            imageInfo = new BadgeInfo()
                            {
                                swapChar = imgPlacementDesignator++,
                                textureIndex = badgeIndex,
                                imageType = ImageType.Badge
                            };
                            _imageInfoByID[badgeIndex] = imageInfo;
                        }
                        var badgeInfo = (BadgeInfo)imageInfo;
                        chatMessage.parsedBadges[badgeInfo.textureIndex] = badgeInfo;

                        // Prepend the users badges to the front of the message
                        var cachedSprite = badgeInfo.GetCachedSprite();
                        messageBuilder.Insert(0, $"{Char.ConvertFromUtf32(badgeInfo.swapChar)}{badgeInfo.spacingString}\u00A0");
                    }
                }
                messageBuilder.Insert(0, "\uFEFF");
                Thread.Sleep(0);
            }
        }
    }

    public static class YouTubeMessageExtensions
    {
        public static void Parse(this YouTubeMessage message, ref char imgPlacementDesignator, ref StringBuilder messageBuilder, ChatMessage chatMessage)
        {
            if (!ImageDownloader.CachedTextures.ContainsKey(chatMessage.origMessage.user.YouTube.profileImageUrl))
                ImageDownloader.Instance.Queue(new TextureDownloadInfo(chatMessage.origMessage.user.YouTube.profileImageUrl, ImageType.YouTube_Profile, chatMessage.origMessage.id, true));

            BadgeInfo swapInfo = new BadgeInfo()
            {
                swapChar = imgPlacementDesignator++,
                textureIndex = chatMessage.origMessage.user.YouTube.profileImageUrl,
                imageType = ImageType.YouTube_Profile
            };
            chatMessage.parsedBadges[swapInfo.textureIndex] = swapInfo;
        }
    }


    public class MessageParser : MonoBehaviour
    {


        private static Dictionary<int, string> _userColors = new Dictionary<int, string>();

        private static StringBuilder _messageBuilder = new StringBuilder(1000);
        private static readonly Dictionary<string, EmoteInfo> _emojiInfoByName = new Dictionary<string, EmoteInfo>();
        private static char imgPlacementDesignator = (char)0xE000;

        public static async void Parse(ChatMessage chatMessage)
        {
            _messageBuilder.Clear();

            // Emojis
            chatMessage.displayMsg = EmojiUtilities.emojiRegex.Replace(chatMessage.displayMsg, (m) =>
            {
                string emojiIndex = EmojiUtilities.WebParseEmojiRegExMatchEvaluator(m);
                string replaceString = m.Value;

                // Couldn't find a match, don't replace it
                if (emojiIndex == String.Empty)
                    return m.Value;

                emojiIndex += ".png";
                if (!ImageDownloader.CachedTextures.ContainsKey(emojiIndex))
                    ImageDownloader.Instance.Queue(new TextureDownloadInfo(emojiIndex, ImageType.Emoji, chatMessage.origMessage.id));

                if (!_emojiInfoByName.TryGetValue(emojiIndex, out var emojiInfo))
                {
                    emojiInfo = new EmoteInfo()
                    {
                        imageType = ImageType.Emoji,
                        swapChar = imgPlacementDesignator++,
                        swapString = replaceString,
                        textureIndex = emojiIndex
                    };
                    _emojiInfoByName[emojiIndex] = emojiInfo;
                }
                chatMessage.parsedEmotes[emojiInfo.textureIndex] = emojiInfo;
                var cachedSprite = emojiInfo.GetCachedSprite();
                return $"\ufeff{Char.ConvertFromUtf32(emojiInfo.swapChar)}{emojiInfo.spacingString}";
            });

            _messageBuilder.Append(chatMessage.displayMsg);

            bool isActionMessage = false;
            if (chatMessage.origMessage is TwitchMessage)
            {
                chatMessage.origMessage.Twitch.ParseEmotes(ref imgPlacementDesignator, ref _messageBuilder, out isActionMessage, chatMessage);
            }
            else if(chatMessage.origMessage is YouTubeMessage)
            {
                chatMessage.origMessage.YouTube.Parse(ref imgPlacementDesignator, ref _messageBuilder, chatMessage);
            }

            string displayColor = chatMessage.origMessage.user.color;
            int nameHash = chatMessage.origMessage.user.displayName.GetHashCode();
            // If the user doesn't already have a displayColor, generate one and store it for later user
            if (string.IsNullOrEmpty(displayColor) && !_userColors.TryGetValue(nameHash, out displayColor))
            {
                // Generate a random color
                Random rand = new Random(nameHash);
                int r = rand.Next(255);
                int g = rand.Next(255);
                int b = rand.Next(255);

                // Convert it to a pastel color
                System.Drawing.Color pastelColor = Drawing.GetPastelShade(System.Drawing.Color.FromArgb(255, r, g, b));
                int argb = ((int)pastelColor.R << 16) + ((int)pastelColor.G << 8) + (int)pastelColor.B;
                string colorString = String.Format("#{0:X6}", argb) + "FF";
                _userColors.Add(nameHash, colorString);
                displayColor = colorString;
            }
            // Set the final displayColor to our message
            chatMessage.displayColor = displayColor;

            _messageBuilder.Insert(0, $"<color={chatMessage.displayColor}><b>{chatMessage.origMessage.user.displayName}</b>{(!isActionMessage ? "</color>:" : "")} ");
            _messageBuilder.Append($"{(isActionMessage ? "</color>" : "")}");

            if(chatMessage.origMessage is TwitchMessage)
            {
                chatMessage.origMessage.Twitch.ParseBadges(ref imgPlacementDesignator, ref _messageBuilder, chatMessage);
            }

            // Finally, push the final message into the render queue
            chatMessage.displayMsg = _messageBuilder.ToString();
            ChatHandler.RenderQueue.Enqueue(chatMessage);
        }
    };
}
