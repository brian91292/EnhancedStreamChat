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
using StreamCore.YouTube;

namespace EnhancedStreamChat.Chat
{
    public class YouTubeMessageParser : MonoBehaviour
    {
        private static Dictionary<int, string> _userColors = new Dictionary<int, string>();
        public static void Parse(ChatMessage newChatMessage)
        {
            List<EmoteInfo> parsedEmotes = new List<EmoteInfo>();
            List<BadgeInfo> parsedBadges = new List<BadgeInfo>();

            char swapChar = (char)0xE000;
            StringBuilder emojilessMessageBuilder = new StringBuilder(newChatMessage.origMessage.message);
            // Parse and download any emojis included in the message
            var matches = EmojiUtilities.GetEmojisInString(newChatMessage.origMessage.message);
            if (matches.Count > 0)
            {
                List<string> foundEmojis = new List<string>();
                foreach (Match m in matches)
                {
                    string emojiIndex = EmojiUtilities.WebParseEmojiRegExMatchEvaluator(m);
                    string replaceString = m.Value;

                    // Build up a copy of the message with no emojis so we can parse out our twitch emotes properly
                    emojilessMessageBuilder = emojilessMessageBuilder.Replace(m.Value, " ");

                    if (emojiIndex != String.Empty)
                    {
                        emojiIndex += ".png";
                        if (!ImageDownloader.CachedTextures.ContainsKey(emojiIndex))
                            ImageDownloader.Instance.Queue(new TextureDownloadInfo(emojiIndex, ImageType.Emoji, newChatMessage.origMessage.id));

                        if (!foundEmojis.Contains(emojiIndex))
                        {
                            foundEmojis.Add(emojiIndex);
                            EmoteInfo swapInfo = new EmoteInfo();
                            swapInfo.imageType = ImageType.Emoji;
                            swapInfo.isEmoji = true;
                            swapInfo.swapChar = swapChar;
                            swapInfo.swapString = replaceString;
                            swapInfo.textureIndex = emojiIndex;
                            parsedEmotes.Add(swapInfo);
                            swapChar++;
                        }
                    }
                }
                parsedEmotes = parsedEmotes.OrderByDescending(o => o.swapString.Length).ToList();
                Thread.Sleep(5);
            }

            // Parse and download any BTTV/FFZ emotes and cheeremotes in the message
            string[] msgParts = newChatMessage.origMessage.message.Split(' ').Distinct().ToArray();
            foreach (string w in msgParts)
            {
                string word = w;
                //Plugin.Log($"WORD: {word}");
                string textureIndex = String.Empty;
                ImageType imageType = ImageType.None;
                if (ImageDownloader.BTTVEmoteIDs.ContainsKey(word))
                {
                    if (ChatConfig.Instance.ShowBTTVEmotes)
                    {
                        textureIndex = $"B{ImageDownloader.BTTVEmoteIDs[word]}";
                        imageType = ImageType.BTTV;
                    }
                }
                else if (ImageDownloader.BTTVAnimatedEmoteIDs.ContainsKey(word))
                {
                    if (ChatConfig.Instance.ShowBTTVEmotes)
                    {
                        textureIndex = $"AB{ImageDownloader.BTTVAnimatedEmoteIDs[word]}";
                        imageType = ImageType.BTTV_Animated;
                    }
                }
                else if (ImageDownloader.FFZEmoteIDs.ContainsKey(word))
                {
                    if (ChatConfig.Instance.ShowFFZEmotes)
                    {
                        textureIndex = $"F{ImageDownloader.FFZEmoteIDs[word]}";
                        imageType = ImageType.FFZ;
                    }
                }

                if (imageType != ImageType.None)
                {
                    if (!ImageDownloader.CachedTextures.ContainsKey(textureIndex))
                        ImageDownloader.Instance.Queue(new TextureDownloadInfo(textureIndex, imageType, newChatMessage.origMessage.id));

                    EmoteInfo swapInfo = new EmoteInfo();
                    swapInfo.imageType = imageType;
                    swapInfo.swapChar = swapChar;
                    swapInfo.swapString = word;
                    swapInfo.textureIndex = textureIndex;
                    parsedEmotes.Add(swapInfo);
                    swapChar++;
                }
            }
            Thread.Sleep(5);

            string[] parts = newChatMessage.origMessage.message.Split(' ');

            // Then replace our emojis after all the emotes are handled, since these aren't sensitive to spacing
            StringBuilder sb = new StringBuilder(string.Join(" ", parts));
            foreach (EmoteInfo e in parsedEmotes.Where(e => e.isEmoji))
                sb.Replace(e.swapString, $"\u00A0{Drawing.imageSpacing}{Char.ConvertFromUtf32(e.swapChar)}");
            newChatMessage.displayMsg = sb.ToString();

            Thread.Sleep(5);

            //// TODO: Re-add tagging, why doesn't unity have highlighting in its default rich text markup?
            //// Highlight messages that we've been tagged in
            //if (Plugin._twitchUsername != String.Empty && msg.Contains(Plugin._twitchUsername)) {
            //    msg = $"<mark=#ffff0050>{msg}</mark>";
            //}

            string displayColor = "";
            int nameHash = newChatMessage.origMessage.user.displayName.GetHashCode();
            if (!_userColors.TryGetValue(nameHash, out displayColor))
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
            newChatMessage.displayColor = displayColor;

            // Add the users name to the message with the correct color
            newChatMessage.displayMsg = $"<color={newChatMessage.displayColor}><b>{newChatMessage.origMessage.user.displayName}</b></color>: {newChatMessage.displayMsg}";

            // Prepend the users badges to the front of the message
            StringBuilder badgeStr = new StringBuilder();
            if (parsedBadges.Count > 0)
            {
                parsedBadges.Reverse();
                for (int i = 0; i < parsedBadges.Count; i++)
                    badgeStr.Insert(0, $"\u200A{Drawing.imageSpacing}{Char.ConvertFromUtf32(parsedBadges[i].swapChar)}\u2004");
            }
            badgeStr.Append("\u200A");
            badgeStr.Append(newChatMessage.displayMsg);

            // Finally, store our final message, parsedEmotes and parsedBadges; then render the message
            newChatMessage.displayMsg = badgeStr.ToString();
            newChatMessage.parsedEmotes = parsedEmotes;
            newChatMessage.parsedBadges = parsedBadges;
            newChatMessage.isActionMessage = false;
            ChatHandler.RenderQueue.Enqueue(newChatMessage);
        }
    };
}
