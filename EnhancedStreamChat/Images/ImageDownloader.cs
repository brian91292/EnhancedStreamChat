using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using EnhancedStreamChat.Chat;
using UnityEngine.XR;
using EnhancedStreamChat.UI;
using StreamCore.SimpleJSON;
using System.Text.RegularExpressions;
using EnhancedStreamChat.Config;
using StreamCore;
using EnhancedStreamChat.Images;
using StreamCore.Utils;
using StreamCore.Config;
using StreamCore.Chat;
using StreamCore.Twitch;

namespace EnhancedStreamChat.Textures
{
    public class CachedAnimationData
    {
        public int index = -1;
        public float delay = -1f;
        public Material imageMaterial;
        public Material shadowMaterial;
        public Texture2D textureAtlas;
        public Rect[] uvs = null;

        public CachedAnimationData(int index, Texture2D textureAtlas, Rect[] uvs, float delay)
        {
            this.index = index;
            this.textureAtlas = textureAtlas;
            this.uvs = uvs;
            this.delay = delay;
        }
    }

    public class CachedSpriteData
    {
        public Sprite sprite = null;
        public CachedAnimationData animInfo = null;
        public float width;
        public float height;
        public float aspectRatio
        {
            get
            {
                if(height > 0)
                    return width / height;
                return 1;
            }
        }

        public CachedSpriteData(ImageType type, CachedAnimationData animInfo, float width, float height)
        {
            float size = Drawing.emoteHeight;
            if (type == ImageType.Badge || type == ImageType.Emoji)
            {
                size = Drawing.badgeEmojiHeight;
            }
            // Adjust the texture width/height if it's not the correct height
            if (height != size)
            {
                float div = height / size;
                width /= div;
                height = size;
            }
            this.animInfo = animInfo;
            this.width = width;
            this.height = height;
        }

        public CachedSpriteData(ImageType type, Sprite sprite, float width, float height)
        {
            float size = Drawing.emoteHeight;
            if (type == ImageType.Badge || type == ImageType.Emoji)
            {
                size = Drawing.badgeEmojiHeight;
            }
            // Adjust the texture width/height if it's not the correct height
            if (height != size)
            {
                float div = height / size;
                width /= div;
                height = size;
            }
            this.sprite = sprite;
            this.width = width;
            this.height = height;
        }
    };

    public class TextureDownloadInfo
    {
        public string spriteIndex;
        public ImageType type;
        public string messageIndex;
        public bool noCache;
        public TextureDownloadInfo(string index, ImageType type, string messageIndex, bool noCache = false)
        {
            this.spriteIndex = index;
            this.type = type;
            this.messageIndex = messageIndex;
            this.noCache = noCache;
        }
    };

    public class TextureSaveInfo
    {
        public string path;
        public byte[] data;
        public TextureSaveInfo(string path, byte[] data)
        {
            this.path = path;
            this.data = data;
        }
    };

    public class ImageDownloader : MonoBehaviour
    {
        public static ConcurrentDictionary<string, string> BTTVEmoteIDs = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> FFZEmoteIDs = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> TwitchBadgeIDs = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> BTTVAnimatedEmoteIDs = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, TwitchCheermote> TwitchCheermoteIDs = new ConcurrentDictionary<string, TwitchCheermote>();
        public static ConcurrentDictionary<string, string> YouTubeProfileImages = new ConcurrentDictionary<string, string>();

        public static ConcurrentDictionary<string, CachedSpriteData> CachedTextures = new ConcurrentDictionary<string, CachedSpriteData>();
        public static ConcurrentQueue<TextureSaveInfo> ImageSaveQueue = new ConcurrentQueue<TextureSaveInfo>();
        private ConcurrentQueue<TextureDownloadInfo> _imageDownloadQueue = new ConcurrentQueue<TextureDownloadInfo>();
        private ConcurrentQueue<TextureDownloadInfo> _animationDownloadQueue = new ConcurrentQueue<TextureDownloadInfo>();
        public static ImageDownloader Instance = null;
        

        public static void OnLoad()
        {
            if (Instance) return;

            new GameObject("EnhancedStreamChatImageDownloader").AddComponent<ImageDownloader>();
        }

        public void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            StartCoroutine(ProcessQueue());
            StartCoroutine(ProcessAnimQueue());
        }

        public void Init()
        {
            BTTVEmoteIDs.Clear();
            FFZEmoteIDs.Clear();
            TwitchBadgeIDs.Clear();
            StartCoroutine(GetEmotes());
        }
        
        public static IEnumerator GetEmotes()
        {
            yield return GetTwitchGlobalBadges();
            yield return GetTwitchChannelBadges();
            yield return GetCheermotes();
            yield return GetBTTVGlobalEmotes();
            yield return GetBTTVChannelEmotes();
            yield return GetFFZGlobalEmotes();
            yield return GetFFZChannelEmotes();
            yield return PreloadAnimatedEmotes();
        }
        
        private static IEnumerator ProcessQueue()
        {
            var waitForEmote = new WaitUntil(() => Instance._imageDownloadQueue.Count > 0);
            while(!Globals.IsApplicationExiting)
            {
                yield return waitForEmote;

                // Download any images that aren't animated
                if (Instance._imageDownloadQueue.TryDequeue(out var imageDownloadInfo))
                {
                    switch (imageDownloadInfo.type)
                    {
                        case ImageType.Twitch:
                            yield return Download($"https://static-cdn.jtvnw.net/emoticons/v1/{imageDownloadInfo.spriteIndex.Substring(1)}/3.0", imageDownloadInfo);
                            break;
                        case ImageType.BTTV:
                            yield return Download($"https://cdn.betterttv.net/emote/{imageDownloadInfo.spriteIndex.Substring(1)}/3x", imageDownloadInfo);
                            break;
                        case ImageType.FFZ:
                            yield return Download($"https://cdn.frankerfacez.com/{imageDownloadInfo.spriteIndex.Substring(1)}", imageDownloadInfo);
                            break;
                        case ImageType.Badge:
                            yield return Download($"https://static-cdn.jtvnw.net/badges/v1/{imageDownloadInfo.spriteIndex}/3", imageDownloadInfo);
                            break;
                        case ImageType.YouTube_Profile:
                            Plugin.Log($"Downloading {imageDownloadInfo.spriteIndex}");
                            yield return Download(imageDownloadInfo.spriteIndex, imageDownloadInfo);
                            break;
                        case ImageType.Emoji:
                            yield return Download(string.Empty, imageDownloadInfo);
                            break;
                    }
                }
            }
        }
        
        private static IEnumerator ProcessAnimQueue()
        {
            var waitForAnimatedEmote = new WaitUntil(() => Instance._animationDownloadQueue.Count > 0);
            while (!Globals.IsApplicationExiting)
            {
                yield return waitForAnimatedEmote;
                // Download animated images separately, so we don't hold up static emotes while processing animations
                if (Instance._animationDownloadQueue.TryDequeue(out var imageDownloadInfo))
                {
                    switch (imageDownloadInfo.type)
                    {
                        case ImageType.BTTV_Animated:
                            yield return DownloadAnimated($"https://cdn.betterttv.net/emote/{imageDownloadInfo.spriteIndex.Substring(2)}/3x", imageDownloadInfo);
                            break;
                        case ImageType.Cheermote:
                            Match match = EmojiUtilities.cheermoteRegex.Match(imageDownloadInfo.spriteIndex);
                            yield return DownloadAnimated($"https://d3aqoihi2n8ty8.cloudfront.net/actions/{(match.Groups["Prefix"].Value)}/dark/animated/{(match.Groups["Value"].Value)}/4.gif", imageDownloadInfo);
                            break;
                    }
                }
            }
        }

        public void Queue(TextureDownloadInfo emote)
        {
            if(emote.type == ImageType.BTTV_Animated || emote.type == ImageType.Cheermote)
                _animationDownloadQueue.Enqueue(emote);
            else
                _imageDownloadQueue.Enqueue(emote);
        }

        private static bool ImageExistsLocally(ref string imagePath, TextureDownloadInfo imageDownloadInfo, out string localFilePath)
        {
            string origImagePath = imagePath;

            if (imageDownloadInfo.noCache)
            {
                localFilePath = "";
                return false;
            }

            string currentPath = Path.Combine(Environment.CurrentDirectory, "Cache", "Images", ImageTypeNames.Get(imageDownloadInfo.type));
            if (!Directory.Exists(currentPath))
                Directory.CreateDirectory(currentPath);

            localFilePath = Path.Combine(currentPath, imageDownloadInfo.spriteIndex);
            if (File.Exists(localFilePath))
            {
                imagePath = localFilePath;
                return true;
            }
            return false;
        }

        public static IEnumerator DownloadAnimated(string imagePath, TextureDownloadInfo imageDownloadInfo, bool isRetry = false)
        {
            if (!CachedTextures.ContainsKey(imageDownloadInfo.spriteIndex))
            {
                byte[] animData = null;
                int _waitForFrames = 10;
                bool localPathExists = ImageExistsLocally(ref imagePath, imageDownloadInfo, out var localFilePath);
                
                yield return Utilities.Download(imagePath, Utilities.DownloadType.Raw, null, (web) =>
                {
                    animData = web.downloadHandler.data;
                });

                if (animData != null)
                {
                    CachedTextures.TryAdd(imageDownloadInfo.spriteIndex, null);
                    yield return AnimationDecoder.Process(animData, ChatHandler.Instance.OverlayAnimatedImage, imageDownloadInfo);
                    if (!localPathExists && !imageDownloadInfo.noCache)
                        ImageSaveQueue.Enqueue(new TextureSaveInfo(localFilePath, animData));
                }

                yield return new WaitUntil(() => _waitForFrames-- == 0);
            }
        }

        public static IEnumerator Download(string imagePath, TextureDownloadInfo imageDownloadInfo, bool isRetry = false)
        {
            if (!CachedTextures.ContainsKey(imageDownloadInfo.spriteIndex))
            {
                int _waitForFrames = 5;
                Sprite sprite = null;
                if (imageDownloadInfo.type != ImageType.Emoji)
                {
                    bool localPathExists = ImageExistsLocally(ref imagePath, imageDownloadInfo, out var localFilePath);
                    yield return Utilities.Download(imagePath, Utilities.DownloadType.Texture,  null, (web) =>
                    {
                        sprite = Utilities.LoadSpriteFromTexture(DownloadHandlerTexture.GetContent(web));
                        if (sprite)
                        {
                            if (!localPathExists && !imageDownloadInfo.noCache)
                                ImageSaveQueue.Enqueue(new TextureSaveInfo(localFilePath, web.downloadHandler.data));
                        }
                    });
                }
                else
                    sprite = Utilities.LoadSpriteFromResources($"EnhancedStreamChat.Resources.Emojis.{imageDownloadInfo.spriteIndex.ToLower()}");

                if (sprite)
                {
                    CachedTextures.TryAdd(imageDownloadInfo.spriteIndex, new CachedSpriteData(imageDownloadInfo.type, sprite, sprite.texture.width, sprite.texture.height));
                    yield return null;
                    ChatHandler.Instance.OverlayImage(sprite, imageDownloadInfo);
                }

                yield return new WaitUntil(() => _waitForFrames-- == 0);
            }
        }
        
        public static IEnumerator PreloadAnimatedEmotes()
        {
            int count = 0;
            foreach (string emoteIndex in BTTVAnimatedEmoteIDs.Values)
            {
                if (!Globals.IsAtMainMenu)
                    yield return new WaitUntil(() => Globals.IsAtMainMenu);

                if (!CachedTextures.ContainsKey(emoteIndex))
                {
                    TextureDownloadInfo downloadInfo = new TextureDownloadInfo("AB" + emoteIndex, ImageType.BTTV_Animated, "!NOTSET!");
                    //Plugin.Log($"Precaching {emoteIndex}");
                    Instance.Queue(downloadInfo);
                    count++;
                    yield return new WaitUntil(() => !Instance._animationDownloadQueue.Contains(downloadInfo));
                }
            }
            Plugin.Log($"Precached {count.ToString()} animated emotes successfully!");
        }

        public static IEnumerator GetCheermotes()
        {
            Plugin.Log($"Downloading twitch cheermote listing");
            int emotesCached = 0;
            yield return Utilities.Download($"https://api.twitch.tv/v5/bits/actions?client_id=jg6ij5z8mf8jr8si22i5uq8tobnmde&channel_id={TwitchWebSocketClient.ChannelInfo[TwitchLoginConfig.Instance.TwitchChannelName].roomId}&include_sponsored=1", Utilities.DownloadType.Raw, null,
                (web) =>
                {
                    JSONNode json = JSON.Parse(web.downloadHandler.text);
                    if (json["actions"].IsArray)
                    {
                        foreach (JSONNode node in json["actions"].AsArray.Values)
                        {
                            TwitchCheermote cheermote = new TwitchCheermote();
                            string prefix = node["prefix"].ToString().ToLower();
                            foreach (JSONNode tier in node["tiers"].Values)
                            {
                                CheermoteTier newTier = new CheermoteTier();
                                newTier.minBits = tier["min_bits"].AsInt;
                                newTier.color = tier["color"];
                                newTier.canCheer = tier["can_cheer"].AsBool;
                                cheermote.tiers.Add(newTier);
                            }
                            cheermote.tiers = cheermote.tiers.OrderBy(t => t.minBits).ToList();
                            TwitchCheermoteIDs.TryAdd(prefix.Substring(1, prefix.Length - 2), cheermote);
                            //Plugin.Log($"Cheermote: {prefix}");
                            emotesCached++;
                        }
                    }
                }
            );
            Plugin.Log($"Web request completed, {emotesCached.ToString()} twitch cheermotes now cached!");
        }

        public static IEnumerator GetTwitchGlobalBadges()
        {
            Plugin.Log($"Downloading twitch global badge listing");
            int emotesCached = 0;
            yield return Utilities.Download("https://badges.twitch.tv/v1/badges/global/display", Utilities.DownloadType.Raw, null, (web) =>
            {
                JSONNode json = JSON.Parse(web.downloadHandler.text);
                if (json["badge_sets"].IsObject)
                {
                    foreach (KeyValuePair<string, JSONNode> kvp in json["badge_sets"])
                    {
                        string name = kvp.Key;
                        JSONObject badge = kvp.Value.AsObject;
                        foreach (KeyValuePair<string, JSONNode> version in badge["versions"].AsObject)
                        {
                            JSONObject versionObject = version.Value.AsObject;
                            string versionID = version.Key;
                            string url = versionObject["image_url_4x"];
                            string index = url.Substring(url.IndexOf("/v1/") + 4).Replace("/3", "");
                            string finalName = $"{name}{versionID}";
                            //Plugin.Log($"Badge: {finalName}, URL: {url}");
                            TwitchBadgeIDs.TryAdd(finalName, index);
                            emotesCached++;
                        }
                    }
                }
            });
            Plugin.Log($"Web request completed, {emotesCached.ToString()} twitch global badges now cached!");
        }

        public static IEnumerator GetTwitchChannelBadges()
        {
            yield return new WaitUntil(() => TwitchWebSocketClient.IsChannelValid);

            Plugin.Log($"Downloading twitch channel badge listing");
            int emotesCached = 0;
            yield return Utilities.Download($"https://badges.twitch.tv/v1/badges/channels/{TwitchWebSocketClient.ChannelInfo[TwitchLoginConfig.Instance.TwitchChannelName].roomId}/display", Utilities.DownloadType.Raw, null, (web) =>
            {
                JSONNode json = JSON.Parse(web.downloadHandler.text);
                if (json["badge_sets"]["subscriber"].IsObject)
                {
                    string name = "subscriber";
                    JSONObject badge = json["badge_sets"]["subscriber"]["versions"].AsObject;
                    foreach (KeyValuePair<string, JSONNode> version in badge)
                    {
                        string versionID = version.Key;
                        JSONObject versionObject = version.Value.AsObject;
                        string url = versionObject["image_url_4x"];
                        string index = url.Substring(url.IndexOf("/v1/") + 4).Replace("/3", "");
                        string finalName = $"{name}{versionID}";
                        if (!TwitchBadgeIDs.TryAdd(finalName, index) && name == "subscriber")
                        {
                            // Overwrite the affiliate sub badges if the channel has any custom ones
                            if (TwitchBadgeIDs.TryGetValue(finalName, out var existing))
                            {
                                TwitchBadgeIDs[finalName] = index;
                                Plugin.Log("Replaced default sub icon!");
                            }
                        }
                        //Plugin.Log($"Badge: {finalName}");
                        emotesCached++;
                    }
                }
            });
            Plugin.Log($"Web request completed, {emotesCached.ToString()} twitch channel badges now cached!");
        }

        public static IEnumerator GetBTTVGlobalEmotes()
        {
            Plugin.Log("Downloading BTTV global emote listing");
            int emotesCached = 0;
            yield return Utilities.Download("https://api.betterttv.net/2/emotes", Utilities.DownloadType.Raw, null, (web) =>
            {
                JSONNode json = JSON.Parse(web.downloadHandler.text);
                if (json["status"].AsInt == 200)
                {
                    JSONArray emotes = json["emotes"].AsArray;
                    foreach (JSONObject o in emotes)
                    {
                        if (o["channel"] == null)
                        {
                            if (o["imageType"] != "gif")
                                BTTVEmoteIDs.TryAdd(o["code"], o["id"]);
                            else
                                BTTVAnimatedEmoteIDs.TryAdd(o["code"], o["id"]);
                            emotesCached++;
                        }
                    }
                }
            });
            Plugin.Log($"Web request completed, {emotesCached.ToString()} BTTV global emotes now cached!");
        }

        public static IEnumerator GetBTTVChannelEmotes()
        {
            Plugin.Log($"Downloading BTTV emotes for channel {TwitchLoginConfig.Instance.TwitchChannelName}");
            int emotesCached = 0;
            yield return Utilities.Download($"https://api.betterttv.net/2/channels/{TwitchLoginConfig.Instance.TwitchChannelName}", Utilities.DownloadType.Raw, null, (web) =>
            {
                JSONNode json = JSON.Parse(web.downloadHandler.text);
                if (json["status"].AsInt == 200)
                {
                    JSONArray emotes = json["emotes"].AsArray;
                    foreach (JSONObject o in emotes)
                    {
                        if (o["imageType"] != "gif")
                            BTTVEmoteIDs.TryAdd(o["code"], o["id"]);
                        else
                            BTTVAnimatedEmoteIDs.TryAdd(o["code"], o["id"]);
                        emotesCached++;
                    }
                }
            });
            Plugin.Log($"Web request completed, {emotesCached.ToString()} BTTV channel emotes now cached!");
        }
        
        public static IEnumerator GetFFZGlobalEmotes()
        {
            Plugin.Log("Downloading FFZ global emote listing");
            int emotesCached = 0;
            yield return Utilities.Download("https://api.frankerfacez.com/v1/set/global", Utilities.DownloadType.Raw, null, (web) =>
            {
                JSONNode json = JSON.Parse(web.downloadHandler.text);
                if (json["sets"].IsObject)
                {
                    JSONArray emotes = json["sets"]["3"]["emoticons"].AsArray;
                    foreach (JSONObject o in emotes)
                    {
                        JSONObject urls = o["urls"].AsObject;
                        string url = urls[urls.Count - 1];
                        string index = url.Substring(url.IndexOf(".com/") + 5);
                        FFZEmoteIDs.TryAdd(o["name"], index);
                        emotesCached++;
                    }
                }
            });
            Plugin.Log($"Web request completed, {emotesCached.ToString()} FFZ global emotes now cached!");
        }
        public static IEnumerator GetFFZChannelEmotes()
        {
            Plugin.Log($"Downloading FFZ emotes for channel {TwitchLoginConfig.Instance.TwitchChannelName}");
            int emotesCached = 0;
            yield return Utilities.Download($"https://api.frankerfacez.com/v1/room/{TwitchLoginConfig.Instance.TwitchChannelName}", Utilities.DownloadType.Raw, null, (web) =>
            {
                JSONNode json = JSON.Parse(web.downloadHandler.text);
                if (json["sets"].IsObject)
                {
                    JSONArray emotes = json["sets"][json["room"]["set"].ToString()]["emoticons"].AsArray;
                    foreach (JSONObject o in emotes)
                    {
                        JSONObject urls = o["urls"].AsObject;
                        string url = urls[urls.Count - 1];
                        string index = url.Substring(url.IndexOf(".com/") + 5);
                        FFZEmoteIDs.TryAdd(o["name"], index);
                        emotesCached++;
                    }
                }
            });
            Plugin.Log($"Web request completed, {emotesCached.ToString()} FFZ channel emotes now cached!");
        }
    };
}
