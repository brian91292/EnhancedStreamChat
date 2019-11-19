using EnhancedStreamChat.Chat;
using EnhancedStreamChat.Textures;
using EnhancedStreamChat.UI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRUIControls;
using EnhancedStreamChat.Config;
using StreamCore.Chat;
using StreamCore;
using StreamCore.Config;
using StreamCore.Utils;
using System.Text.RegularExpressions;
using EnhancedStreamChat.Images;
using StreamCore.YouTube;
using StreamCore.Twitch;
using System.Text;
using System.Diagnostics;

namespace EnhancedStreamChat
{
    public class ChatHandler : MonoBehaviour, ITwitchIntegration, IYouTubeIntegration
    {
        public static ChatHandler Instance = null;
        public static ConcurrentQueue<ChatMessage> RenderQueue = new ConcurrentQueue<ChatMessage>();
        public static Func<TwitchMessage, bool> ChatMessageFilters;
        
        public Image lockButtonImage;
        public Image background;
        public GameObject lockButtonPrimitive;
        public GameObject chatMoverPrimitive;
        public bool initialized = false;
        public ObjectPool<CustomImage> imagePool;

        private Canvas _twitchChatCanvas = null;
        private Queue<CustomText> _chatMessages = new Queue<CustomText>();
        private Transform _chatMoverCube;
        private Transform _lockButtonSphere;
        private float _currentBackgroundHeight;
        private RectTransform _canvasRectTransform;
        private Sprite _lockedSprite;
        private Sprite _unlockedSprite;
        private bool _messageRendering = false;
        private int _waitForFrames = 0;
        private bool _configChanged = false;
        private ConcurrentQueue<KeyValuePair<string, bool>> _timeoutQueue = new ConcurrentQueue<KeyValuePair<string, bool>>();
        private ChatMover _movePointer = null;
        private LockToggle _lockPointer = null;
        private string _lastFontName;
        private CustomText _testMessage = null;
        private readonly WaitUntil _delay = new WaitUntil(() => { return Instance._waitForFrames == 0; });
        private bool _hasDisplayedTwitchStatus = false;
        private string _lastRoomId = String.Empty;
        private DateTime _statusDisplayDelayTime = DateTime.Now;

        bool IGenericChatIntegration.IsPluginReady { get; set; }
        //public bool IsPluginReady { get; set; } = false;

        public void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register our message handlers
            RegisterMessageHandlers();

            // Startup the texture downloader and anim controller
            ImageDownloader.OnLoad();
            AnimationController.OnLoad();

            // Initialize the chats UI
            InitializeChatUI();

            // Subscribe to events
            ChatConfig.Instance.ConfigChangedEvent += ChatConfigChanged;

            initialized = true;

            ((IGenericChatIntegration)this).IsPluginReady = true;
            Plugin.Log("EnhancedStreamChat initialized");
        }

        public void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            var vrPointers = Resources.FindObjectsOfTypeAll<VRPointer>();
            if (vrPointers.Count() == 0)
            {
                Plugin.Log("Failed to get VRPointer!");
                return;
            }
            var _vrPointer = to.name != "GameCore" ? vrPointers.First() : vrPointers.Last();
            if (_movePointer)
                Destroy(_movePointer);
            _movePointer = _vrPointer.gameObject.AddComponent<ChatMover>();
            _movePointer.Init(_chatMoverCube);

            if (_lockPointer)
                Destroy(_lockPointer);
            _lockPointer = _vrPointer.gameObject.AddComponent<LockToggle>();
            _lockPointer.Init(lockButtonImage, _lockButtonSphere);
            Plugin.Log($"{from.name} -> {to.name}");
        }

        private void ChatConfigChanged(ChatConfig config)
        {
            _configChanged = true;
        }

        private void OnConfigChanged()
        {
            _configChanged = false;
            if (ChatConfig.Instance.FontName != _lastFontName)
            {
                StartCoroutine(Drawing.Initialize(gameObject.transform));
                foreach (CustomText currentMessage in _chatMessages)
                {
                    Font f = currentMessage.font;
                    currentMessage.font = Drawing.LoadSystemFont(ChatConfig.Instance.FontName);
                    currentMessage.color = ChatConfig.Instance.TextColor;
                    Destroy(f);
                }
                _lastFontName = ChatConfig.Instance.FontName;
            }

            UpdateChatUI();
            _canvasRectTransform.localScale = new Vector3(0.012f * ChatConfig.Instance.ChatScale, 0.012f * ChatConfig.Instance.ChatScale, 0.012f * ChatConfig.Instance.ChatScale);
            _lockButtonSphere.localScale = new Vector3(0.15f * ChatConfig.Instance.ChatScale, 0.15f * ChatConfig.Instance.ChatScale, 0.001f * ChatConfig.Instance.ChatScale);
            background.color = ChatConfig.Instance.BackgroundColor;
        }

        private void HandleStatusMessages()
        {
            // Wait a few seconds after we've connect to the chat, then send our welcome message
            if (!_hasDisplayedTwitchStatus && TwitchWebSocketClient.Initialized)
            {
                // If the last room id hasn't been set, allow up to a 30 second timeout before we throw an error
                if ((DateTime.Now - _statusDisplayDelayTime).TotalSeconds < 30)
                {
                    return;
                }

                string msg = null;
                if (TwitchWebSocketClient.Connected && TwitchWebSocketClient.LoggedIn)
                {
                    if (!TwitchWebSocketClient.IsChannelValid && TwitchLoginConfig.Instance.TwitchChannelName != "")
                    {
                        msg = $"<color=#FF0000FF>Twitch Error: Failed to join channel \"{TwitchLoginConfig.Instance.TwitchChannelName}\". Please enter a valid Twitch channel name in the Enhanced Stream Chat settings submenu, or manually in TwitchLoginInfo.ini, then try again.</color>";
                    }
                }
                else
                    msg = "<color=#FF0000FF>Twitch Error: Twitch login attempt failed. Ensure your login data is correct in UserData\\StreamCore\\TwitchLoginInfo.ini, then try again.</color>";

                if(msg != null)
                    RenderQueue.Enqueue(new ChatMessage(msg, new GenericChatMessage()));

                _hasDisplayedTwitchStatus = true;
            }
        }

        public void Update()
        {
            if (Drawing.MaterialsCached)
            {
                HandleStatusMessages();

                if (_configChanged)
                    OnConfigChanged();

                // Make sure to delete any purged messages right away
                if (_timeoutQueue.Count > 0 && _timeoutQueue.TryDequeue(out var id))
                    PurgeChatMessagesInternal(id);

                if (_waitForFrames > 0)
                {
                    _waitForFrames--;
                    return;
                }

                //// Wait try to display any new chat messages if our fps is tanking
                //float fps = 1.0f / Time.deltaTime;
                //if (!Plugin.Instance.IsAtMainMenu && fps < XRDevice.refreshRate - 5)
                //    return;

                // Display any messages that we've cached all the resources for and prepared for rendering
                if (RenderQueue.Count > 0 && !_messageRendering)
                {
                    if (RenderQueue.TryDequeue(out var messageToSend))
                    {
                        if (ChatConfig.Instance.FilterBroadcasterMessages && (messageToSend.origMessage.user.Twitch.isBroadcaster || messageToSend.origMessage.user.YouTube.isChatOwner))
                            return;
                        if (ChatConfig.Instance.FilterCommandMessages && messageToSend.origMessage.message.StartsWith("!"))
                            return;
                        if (ChatConfig.Instance.FilterSelfMessages && messageToSend.origMessage.user.id == TwitchWebSocketClient.OurTwitchUser.id)
                            return;

                        if (ChatMessageFilters != null)
                        {
                            foreach (var filter in ChatMessageFilters.GetInvocationList())
                            {
                                try
                                {
                                    var ret = (bool)filter?.DynamicInvoke(messageToSend.origMessage);
                                    if (ret)
                                        return;
                                }
                                catch (Exception ex)
                                {
                                    Plugin.Log(ex.ToString());
                                }
                            }
                        }
                        StartCoroutine(AddNewChatMessage(messageToSend.displayMsg, messageToSend));
                    }
                }
                // Save images to file when we're at the main menu
                else if (Globals.IsAtMainMenu && ImageDownloader.ImageSaveQueue.Count > 0 && ImageDownloader.ImageSaveQueue.TryDequeue(out var saveInfo))
                {
                    File.WriteAllBytes(saveInfo.path, saveInfo.data);
                }
            }
        }

        public void LateUpdate()
        {
            if (Drawing.MaterialsCached)
            {
                _twitchChatCanvas.transform.eulerAngles = ChatConfig.Instance.ChatRotation;
                _twitchChatCanvas.transform.position = ChatConfig.Instance.ChatPosition;
                if (!ChatConfig.Instance.ReverseChatOrder) _twitchChatCanvas.transform.position = _twitchChatCanvas.transform.TransformPoint(new Vector3(0, _currentBackgroundHeight));

                _chatMoverCube.localScale = background.rectTransform.sizeDelta * (ChatConfig.Instance.ChatScale * 1.2f) / Drawing.pixelsPerUnit;
                _chatMoverCube.eulerAngles = ChatConfig.Instance.ChatRotation;
                _chatMoverCube.position = background.rectTransform.TransformPoint(background.rectTransform.rect.width / 2, _currentBackgroundHeight / 2, 0);

                Vector3[] LocalCorners = new Vector3[4];
                background.rectTransform.GetLocalCorners(LocalCorners);
                _lockButtonSphere.eulerAngles = ChatConfig.Instance.ChatRotation;
                lockButtonImage.rectTransform.eulerAngles = ChatConfig.Instance.ChatRotation;
                lockButtonImage.rectTransform.position = background.rectTransform.TransformPoint((ChatConfig.Instance.ReverseChatOrder ? LocalCorners[2] : LocalCorners[3]) - new Vector3(lockButtonImage.rectTransform.sizeDelta.x / 2, lockButtonImage.rectTransform.sizeDelta.y / 2));
                _lockButtonSphere.position = lockButtonImage.rectTransform.TransformPoint(new Vector3(lockButtonImage.preferredWidth / Drawing.pixelsPerUnit, lockButtonImage.preferredHeight / Drawing.pixelsPerUnit, 0));
            }
        }

        private void RegisterMessageHandlers()
        {

            YouTubeMessageHandlers.OnInitialize += () =>
            {
                RenderQueue.Enqueue(new ChatMessage("Connecting to YouTube chat...", new GenericChatMessage()));
            };

            YouTubeMessageHandlers.OnConnectedToLiveChat += (liveBroadcastInfo) =>
            {
                RenderQueue.Enqueue(new ChatMessage($"Success joining YouTube channel \"{YouTubeLiveBroadcast.channelName}\"", new GenericChatMessage()));
                RenderQueue.Enqueue(new ChatMessage($"Current YouTube Broadcast: \"{liveBroadcastInfo.snippet.title}\"", new GenericChatMessage()));
            };

            YouTubeMessageHandlers.OnMessageReceived += (youTubeMsg) =>
            {
                MessageParser.Parse(new ChatMessage(Utilities.EscapeHTML(youTubeMsg.message), youTubeMsg));
            };

            YouTubeMessageHandlers.OnYouTubeError += (error) => {
                RenderQueue.Enqueue(new ChatMessage($"<color=#FF0000FF>YouTube Error: {error}</color>", new GenericChatMessage()));
            };

            TwitchWebSocketClient.OnConfigUpdated += () =>
            {
                _lastRoomId = String.Empty;
                _statusDisplayDelayTime = DateTime.Now;
                _hasDisplayedTwitchStatus = false;
            };

            TwitchWebSocketClient.OnConnected += () =>
            {
                _statusDisplayDelayTime = TwitchWebSocketClient.ConnectionTime;
                if (TwitchLoginConfig.Instance.TwitchChannelName == String.Empty)
                {
                    RenderQueue.Enqueue(new ChatMessage("Welcome to Enhanced Stream Chat! You must configure at least one chat service to continue, read the instructions at https://github.com/brian91292/StreamCore for more information.", new GenericChatMessage()));
                }
                else
                {
                    RenderQueue.Enqueue(new ChatMessage("Connecting to Twitch chat...", new GenericChatMessage()));
                }
            };

            TwitchMessageHandlers.ROOMSTATE += (twitchMsg, twitchChannel) =>
            {
                if (_lastRoomId != twitchChannel.roomId)
                {
                    _lastRoomId = twitchChannel.roomId;
                    RenderQueue.Enqueue(new ChatMessage($"Success joining Twitch channel \"{TwitchLoginConfig.Instance.TwitchChannelName}\"", new GenericChatMessage()));
                    _hasDisplayedTwitchStatus = true;
                    ImageDownloader.Instance.Init();
                }
            };

            TwitchMessageHandlers.PRIVMSG += (genericMessage) =>
            {
                var twitchMsg = genericMessage.Twitch;
                // Don't show any messages that aren't from the channel in the config
                if (twitchMsg.channelName != TwitchLoginConfig.Instance.TwitchChannelName)
                    return;

                Stopwatch s = Stopwatch.StartNew();
                MessageParser.Parse(new ChatMessage(Utilities.EscapeHTML(twitchMsg.message), twitchMsg));
                s.Stop();
                Plugin.Log($"Message parsing took {s.ElapsedMilliseconds}ms");
            };

            TwitchMessageHandlers.USERNOTICE += (twitchMsg) =>
            {
                string msgId = String.Empty, systemMsg = String.Empty;
                foreach (Match t in twitchMsg.tags)
                {
                    switch (t.Groups["Tag"].Value)
                    {
                        case "msg-id":
                            msgId = t.Groups["Value"].Value;
                            break;
                        case "system-msg":
                            systemMsg = t.Groups["Value"].Value.Replace("\\s", " ");
                            break;
                        default:
                            break;
                    }
                }
                switch (msgId)
                {
                    case "sub":
                    case "resub":
                    case "subgift":
                    case "anonsubgift":
                        MessageParser.Parse(new ChatMessage($"{systemMsg.Substring(systemMsg.IndexOf(" ") + 1).Split(new char[] { '\n' }, 2)[0]}", twitchMsg));
                        if (twitchMsg.message != String.Empty)
                            MessageParser.Parse(new ChatMessage(twitchMsg.message, twitchMsg));
                        break;
                    case "raid":
                        break;
                    case "ritual":
                        break;
                }
            };

            TwitchMessageHandlers.USERSTATE += (twitchMsg) =>
            {
                if (!(twitchMsg.user.Twitch.isBroadcaster || twitchMsg.user.Twitch.isMod))
                {
                    TwitchMessage tmpMessage = new TwitchMessage();
                    tmpMessage.user.displayName = "NOTICE";
                    tmpMessage.user.color = "FF0000FF";
                    MessageParser.Parse(new ChatMessage($"Twitch account {twitchMsg.user.displayName} is not a moderator of channel #{twitchMsg.channelName}. The default user rate limit is 20 messages per 30 seconds; to increase this limit to 100, grant this user moderator privileges.", tmpMessage));
                }
            };

            TwitchMessageHandlers.CLEARCHAT += (twitchMsg) =>
            {
                string userId = "!FULLCLEAR!";
                foreach (Match t in twitchMsg.tags)
                {
                    if (t.Groups["Tag"].Value == "target-user-id")
                    {
                        userId = t.Groups["Value"].Value;
                        break;
                    }
                }
                PurgeMessagesFromUser(userId);
            };

            TwitchMessageHandlers.CLEARMSG += (twitchMsg) =>
            {
                string msgId = String.Empty;
                foreach (Match t in twitchMsg.tags)
                {
                    if (t.Groups["Tag"].Value == "target-msg-id")
                    {
                        msgId = t.Groups["Value"].Value;
                        break;
                    }
                }
                if (msgId == String.Empty) return;
                PurgeChatMessageById(msgId);
            };
        }

        private void InitializeChatUI()
        {
            // Precache a pool of images objects that will be used for displaying emotes/badges later on
            imagePool = new ObjectPool<CustomImage>(0,
                // FirstAlloc
                null,
                // OnAlloc
                ((CustomImage image) =>
                {
                    image.shadow.enabled = false;
                }),
                // OnFree
                ((CustomImage image) =>
                {
                    image.material = null;
                    image.enabled = false;
                    image.cachedTextureData.animInfo?.animData?.DecRefs();
                })
            );

            _lastFontName = ChatConfig.Instance.FontName;
            StartCoroutine(Drawing.Initialize(gameObject.transform));

            _lockedSprite = Utilities.LoadSpriteFromResources("EnhancedStreamChat.Resources.LockedIcon.png");
            _lockedSprite.texture.wrapMode = TextureWrapMode.Clamp;
            _unlockedSprite = Utilities.LoadSpriteFromResources("EnhancedStreamChat.Resources.UnlockedIcon.png");
            _unlockedSprite.texture.wrapMode = TextureWrapMode.Clamp;

            _twitchChatCanvas = gameObject.AddComponent<Canvas>();
            _twitchChatCanvas.renderMode = RenderMode.WorldSpace;
            var collider = gameObject.AddComponent<MeshCollider>();
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = Drawing.pixelsPerUnit;
            _canvasRectTransform = _twitchChatCanvas.GetComponent<RectTransform>();
            _canvasRectTransform.localScale = new Vector3(0.012f * ChatConfig.Instance.ChatScale, 0.012f * ChatConfig.Instance.ChatScale, 0.012f * ChatConfig.Instance.ChatScale);

            background = new GameObject("EnhancedStreamChatBackground").AddComponent<Image>();
            background.rectTransform.SetParent(gameObject.transform, false);
            background.color = ChatConfig.Instance.BackgroundColor;
            background.rectTransform.pivot = new Vector2(0, 0);
            background.rectTransform.sizeDelta = new Vector2(ChatConfig.Instance.ChatWidth + ChatConfig.Instance.BackgroundPadding, 0);
            background.rectTransform.localPosition = new Vector3(0 - (ChatConfig.Instance.ChatWidth + ChatConfig.Instance.BackgroundPadding) / 2, 0, 0);

            var lockButtonGameObj = new GameObject("EnhancedStreamChatLockButton");
            lockButtonImage = lockButtonGameObj.AddComponent<Image>();
            lockButtonImage.preserveAspect = true;
            lockButtonImage.rectTransform.sizeDelta = new Vector2(10, 10);
            lockButtonImage.rectTransform.SetParent(gameObject.transform, false);
            lockButtonImage.rectTransform.pivot = new Vector2(0, 0);
            lockButtonImage.color = Color.white.ColorWithAlpha(0.05f);
            lockButtonImage.sprite = ChatConfig.Instance.LockChatPosition ? _lockedSprite : _unlockedSprite;
            lockButtonGameObj.AddComponent<Shadow>();

            chatMoverPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DontDestroyOnLoad(chatMoverPrimitive);
            _chatMoverCube = chatMoverPrimitive.transform;

            lockButtonPrimitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.DontDestroyOnLoad(lockButtonPrimitive);
            _lockButtonSphere = lockButtonPrimitive.transform;
            _lockButtonSphere.localScale = new Vector3(0.15f * ChatConfig.Instance.ChatScale, 0.15f * ChatConfig.Instance.ChatScale, 0.001f);

            while (_chatMessages.Count < ChatConfig.Instance.MaxChatLines)
                _chatMessages.Enqueue(Drawing.InitText("", Color.clear, ChatConfig.Instance.ChatScale, new Vector2(ChatConfig.Instance.ChatWidth, 1), new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0), gameObject.transform, TextAnchor.UpperLeft, false));

            var go = new GameObject();
            DontDestroyOnLoad(go);
            _testMessage = Drawing.InitText("", Color.clear, ChatConfig.Instance.ChatScale, new Vector2(ChatConfig.Instance.ChatWidth, 1), new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0), go.transform, TextAnchor.UpperLeft, true);
            _testMessage.enabled = false;
        }

        private static readonly Regex _htmlTagRegex = new Regex(@"<(?<Tag>[a-z]+)=?(?<Value>[^>=]+)?>", RegexOptions.Compiled | RegexOptions.Multiline);

        private IEnumerator AddNewChatMessage(string origMsg, ChatMessage messageInfo)
        {
            _messageRendering = true;
            CustomText currentMessage = null;

            _testMessage.text = origMsg;
            _testMessage.cachedTextGenerator.Populate(origMsg, _testMessage.GetGenerationSettings(_testMessage.rectTransform.rect.size));
            yield return null;

            Dictionary<string, string> openTags = new Dictionary<string, string>();
            for (int i = 0; i < _testMessage.cachedTextGenerator.lineCount; i++)
            {
                int index = ChatConfig.Instance.ReverseChatOrder ? _testMessage.cachedTextGenerator.lineCount - 1 - i : i;

                string msg;
                if (index < _testMessage.cachedTextGenerator.lineCount - 1)
                    msg = _testMessage.text.Substring(_testMessage.cachedTextGenerator.lines[index].startCharIdx, _testMessage.cachedTextGenerator.lines[index + 1].startCharIdx - _testMessage.cachedTextGenerator.lines[index].startCharIdx);
                else
                    msg = _testMessage.text.Substring(_testMessage.cachedTextGenerator.lines[index].startCharIdx);

                if (msg.IsAllWhitespace())
                {
                    continue;
                }

                if(openTags.Count > 0)
                {
                    foreach(var tag in openTags.ToArray())
                    {
                        msg = msg.Insert(0, $"<{tag.Key}{(tag.Value != null? $"={tag.Value}" : "")}>");
                        var closingTag = $"</{tag.Key}>";
                        if (msg.Contains(closingTag))
                        {
                            openTags.Remove(tag.Key);
                        }
                        else
                        {
                            msg += closingTag;
                        }
                    }
                }

                var matches = _htmlTagRegex.Matches(msg).Cast<Match>().Reverse();
                foreach (Match m in matches)
                {
                    var tag = m.Groups["Tag"].Value;
                    if (openTags.ContainsKey(tag))
                        continue;

                    var closingTag = $"</{tag}>";
                    if (msg.Contains(closingTag))
                        continue;

                    string value = null;
                    if (m.Groups["Value"].Success)
                    {
                        value = m.Groups["Value"].Value;
                    }
                    openTags.Add(tag, value);
                    msg += closingTag;
                }

                currentMessage = _chatMessages.Dequeue();
                currentMessage.hasRendered = false;
                currentMessage.text = msg;
                currentMessage.messageInfo = messageInfo;
                currentMessage.material = Drawing.noGlowMaterialUI;
                currentMessage.color = ChatConfig.Instance.TextColor;
                _chatMessages.Enqueue(currentMessage);

                FreeImages(currentMessage);
                UpdateChatUI();
                yield return null;

                foreach (BadgeInfo b in messageInfo.parsedBadges.Values)
                    Drawing.OverlayImage(currentMessage, b);

                foreach (EmoteInfo e in messageInfo.parsedEmotes.Values)
                    Drawing.OverlayImage(currentMessage, e);

                currentMessage.hasRendered = true;

                _waitForFrames = 5;
                yield return _delay;
            }
            _testMessage.text = "";
            _messageRendering = false;
        }

        public void OverlayImage(Sprite sprite, TextureDownloadInfo imageDownloadInfo)
        {
            try
            {
                string spriteIndex = imageDownloadInfo.spriteIndex;
                string messageIndex = imageDownloadInfo.messageIndex;
                foreach (CustomText currentMessage in _chatMessages)
                {
                    if (currentMessage.messageInfo == null || !currentMessage.hasRendered) continue;

                    foreach (EmoteInfo e in currentMessage.messageInfo.parsedEmotes.Values)
                    {
                        if (e.textureIndex == spriteIndex)
                            Drawing.OverlayImage(currentMessage, e);
                    }

                    foreach (BadgeInfo b in currentMessage.messageInfo.parsedBadges.Values)
                    {
                        if (b.textureIndex == spriteIndex)
                            Drawing.OverlayImage(currentMessage, b);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log($"Exception when overlaying emote! {e.ToString()}");
            }
        }

        public void OverlayAnimatedImage(Texture2D texture, Rect[] uvs, float[] delays, bool isDelayConsistent, int width, int height, TextureDownloadInfo imageDownloadInfo)
        {
            try
            {
                string spriteIndex = imageDownloadInfo.spriteIndex;
                string messageIndex = imageDownloadInfo.messageIndex;
                ImageDownloader.CachedTextures.TryGetValue(spriteIndex, out var cachedTex);
                CachedAnimationData oldAnimInfo = cachedTex?.animInfo;
                   
                // Create the shaders which will cycle through our animation texture sheet
                var animInfo = new CachedAnimationData(uvs.Length > 1 ? AnimationController.Instance.Register(spriteIndex, uvs, delays) : new AnimControllerData(spriteIndex, uvs, delays), texture, uvs, delays);

                // Try to create our animMaterial and shadowMaterial if they don't already exist
                Material _animMaterial = oldAnimInfo?.imageMaterial;
                Material _shadowMaterial = oldAnimInfo?.shadowMaterial;
                if (_animMaterial == null)
                {
                    _animMaterial = Instantiate(Drawing.CropMaterial);
                    _animMaterial.SetVector("_CropFactors", new Vector4(uvs[0].x, uvs[0].y, uvs[0].width, uvs[0].height));
                }
                if (ChatConfig.Instance.DrawShadows)
                {
                    if (_shadowMaterial == null)
                    {
                        _shadowMaterial = Instantiate(Drawing.CropMaterialColorMultiply);
                        _shadowMaterial.SetVector("_CropFactors", new Vector4(uvs[0].x, uvs[0].y, uvs[0].width, uvs[0].height));
                        _shadowMaterial.SetColor("_Color", Color.black.ColorWithAlpha(0.2f));
                        _shadowMaterial.renderQueue = 3001;
                    }
                    _shadowMaterial.mainTexture = texture;
                    animInfo.shadowMaterial = _shadowMaterial;
                }
                _animMaterial.mainTexture = texture;
                animInfo.imageMaterial = _animMaterial;

                var newCachedSpriteData = new CachedSpriteData(imageDownloadInfo.type, animInfo, isDelayConsistent, width, height);
                ImageDownloader.CachedTextures[spriteIndex] = newCachedSpriteData;

                if (cachedTex != null && oldAnimInfo != null && oldAnimInfo.uvs.Length == 1)
                {
                    foreach (CustomText currentMessage in _chatMessages)
                    {
                        for (int i = currentMessage.emoteRenderers.Count - 1; i >= 0; i--)
                        {
                            CustomImage img = currentMessage.emoteRenderers[i];
                            if (img.spriteIndex == spriteIndex)
                            {
                                Plugin.Log("Freeing old emote!");
                                imagePool.Free(img);
                                currentMessage.emoteRenderers.RemoveAt(i);
                            }
                        }
                    }
                }

                foreach (CustomText currentMessage in _chatMessages)
                {
                    if (currentMessage.messageInfo == null || !currentMessage.hasRendered) continue;

                    foreach (EmoteInfo e in currentMessage.messageInfo.parsedEmotes.Values)
                    {
                        if (e.textureIndex == spriteIndex)
                            Drawing.OverlayImage(currentMessage, e);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log($"Exception when overlaying animated emote! {e.ToString()}");
            }
        }

        private bool PurgeChatMessage(CustomText currentMessage)
        {
            string userName = $"<color={currentMessage.messageInfo.displayColor}><b>{currentMessage.messageInfo.origMessage.user.displayName}</b></color>:";
            if (currentMessage.text.Contains(userName))
                currentMessage.text = $"{userName} <message deleted>";
            else
                currentMessage.text = "";

            FreeImages(currentMessage);
            return true;
        }

        private void PurgeChatMessagesInternal(KeyValuePair<string, bool> messageInfo)
        {
            try
            {
                bool isUserId = messageInfo.Value;
                string id = messageInfo.Key;

                if (id == "!FULLCLEAR!" && !ChatConfig.Instance.ClearChatEnabled)
                    return;
                else if (id != "!FULLCLEAR!" && !ChatConfig.Instance.ClearTimedOutMessages)
                    return;

                bool purged = false;
                foreach (CustomText currentMessage in _chatMessages)
                {
                    if (currentMessage.messageInfo == null) continue;

                    // Handle purging messages by user id or by message id, since both are possible
                    if (id == "!FULLCLEAR!" || (isUserId && currentMessage.messageInfo.origMessage.user.id == id) || (!isUserId && currentMessage.messageInfo.origMessage.id == id))
                    {
                        string userName = $"<color={currentMessage.messageInfo.displayColor}><b>{currentMessage.messageInfo.origMessage.user.displayName}</b></color>:";
                        if (currentMessage.text.Contains(userName))
                            currentMessage.text = $"{userName} <message deleted>";
                        else
                            currentMessage.text = "";

                        FreeImages(currentMessage);
                        purged = true;
                    }
                }
                if (purged)
                {
                    if (id == "!FULLCLEAR!")
                        RenderQueue.Enqueue(new ChatMessage("Chat was cleared by a moderator.", new GenericChatMessage()));

                    UpdateChatUI();
                }
            }
            catch(Exception ex)
            {
                Plugin.Log($"An unhandled exception occurred while trying to clear chat message. {ex.ToString()}");
            }
        }

        public void PurgeChatMessageById(string messageId)
        {
            _timeoutQueue.Enqueue(new KeyValuePair<string, bool>(messageId, false));
        }

        public void PurgeMessagesFromUser(string userID)
        {
            _timeoutQueue.Enqueue(new KeyValuePair<string, bool>(userID, true));
        }

        private void FreeImages(CustomText currentMessage)
        {
            if (currentMessage.emoteRenderers.Count > 0)
            {
                foreach (CustomImage image in currentMessage.emoteRenderers)
                    imagePool.Free(image);

                currentMessage.emoteRenderers.Clear();
            }
        }

        private void UpdateChatUI()
        {
            if (_chatMessages.Count > 0)
            {
                // Update the position of each text elem (which also moves the emotes since they are children of the text)
                float currentYValue = 0;

                float initialYValue = currentYValue;
                var _tmpArray = _chatMessages.ToArray();
                for (int i = 0; i < _tmpArray.Length; i++)
                {
                    int index = ChatConfig.Instance.ReverseChatOrder ? _tmpArray.Length - 1 - i : i;
                    if (_tmpArray[index].text != "")
                    {
                        _tmpArray[index].transform.localPosition = new Vector3(-ChatConfig.Instance.ChatWidth / 2, currentYValue, 0);
                        currentYValue -= (_tmpArray[index].preferredHeight + (i < _chatMessages.Count() - 1 ? ChatConfig.Instance.LineSpacing + 1.5f : 0));
                    }
                }
                _currentBackgroundHeight = (initialYValue - currentYValue) + ChatConfig.Instance.BackgroundPadding * 2;
                background.rectTransform.sizeDelta = new Vector2(ChatConfig.Instance.ChatWidth + ChatConfig.Instance.BackgroundPadding * 2, _currentBackgroundHeight);
                background.rectTransform.position = _twitchChatCanvas.transform.TransformPoint(new Vector3(-ChatConfig.Instance.ChatWidth / 2 - ChatConfig.Instance.BackgroundPadding, (initialYValue - _currentBackgroundHeight + ChatConfig.Instance.BackgroundPadding), 0.1f));
            }
        }

        public void UpdateLockButton()
        {
            lockButtonImage.sprite = ChatConfig.Instance.LockChatPosition ? _lockedSprite : _unlockedSprite;
        }
    };
}
