//using EnhancedTwitchChat.Bot;
using IllusionPlugin;
using StreamCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using StreamCore.Config;
using StreamCore;
using System.Threading.Tasks;
using System.Threading;
using BeatSaberMarkupLanguage.Attributes;

namespace EnhancedStreamChat.Config
{
    public class OldConfigOptions
    {
        public string TwitchChannel = "";
    }

    public class OldBlacklistOption
    {
        public string SongBlacklist;
    }

    public class SemiOldConfigOptions
    {
        public string TwitchChannelName = "";
        public string TwitchUsername = "";
        public string TwitchOAuthToken = "";
        public bool SongRequestBot = false;
        public bool PersistentRequestQueue = true;
    }
    
    public partial class ChatConfig : PersistentSingleton<ChatConfig>
    {
        private string FilePath = Path.Combine(Globals.DataPath, $"{Plugin.ModuleName.Replace(" ", "")}.ini");

        // User Interface
        [UIValue("menu-font-name")]
        public string FontName = "Segoe UI";
        [UIValue("chat-scale")]
        public float ChatScale = 1.1f;
        [UIValue("chat-width")]
        public float ChatWidth = 160;
        [UIValue("line-spacing")]
        public float LineSpacing = 2.0f;
        [UIValue("max-chat-lines")]
        public int MaxChatLines = 30;
        [UIValue("background-padding")]
        public float BackgroundPadding = 4;

        // Preferences
        [UIValue("animated-emotes")]
        public bool AnimatedEmotes = true;
        [UIValue("draw-shadows")]
        public bool DrawShadows = false;
        [UIValue("lock-chat-position")]
        public bool LockChatPosition = false;
        [UIValue("reverse-chat-order")]
        public bool ReverseChatOrder = false;
        [UIValue("show-bttv-emotes")]
        public bool ShowBTTVEmotes = true;
        [UIValue("show-ffz-emotes")]
        public bool ShowFFZEmotes = true;

        // Filters
        [UIValue("clear-chat")]
        public bool ClearChatEnabled = true;
        [UIValue("timed-out-messages")]
        public bool ClearTimedOutMessages = true;
        [UIValue("command-messages")]
        public bool FilterCommandMessages = false;
        [UIValue("broadcaster-messages")]
        public bool FilterBroadcasterMessages = false;
        [UIValue("self-messages")]
        public bool FilterSelfMessages = false;

        public float PositionX = 0;
        public float PositionY = 2.6f;
        public float PositionZ = 2.3f;

        public float RotationX = -30;
        public float RotationY = 0;
        public float RotationZ = 0;

        public float TextColorR = 1;
        public float TextColorG = 1;
        public float TextColorB = 1;
        public float TextColorA = 1;

        public float BackgroundColorR = 0;
        public float BackgroundColorG = 0;
        public float BackgroundColorB = 0;
        public float BackgroundColorA = 0.6f;
      
        public event Action<ChatConfig> ConfigChangedEvent;

        private readonly FileSystemWatcher _configWatcher;
        private bool _saving;

        public Color TextColor
        {
            get
            {
                return new Color(TextColorR, TextColorG, TextColorB, TextColorA);
            }
            set
            {
                TextColorR = value.r;
                TextColorG = value.g;
                TextColorB = value.b;
                TextColorA = value.a;
            }
        }

        public Color BackgroundColor
        {
            get
            {
                return new Color(BackgroundColorR, BackgroundColorG, BackgroundColorB, BackgroundColorA);
            }
            set
            {
                BackgroundColorR = value.r;
                BackgroundColorG = value.g;
                BackgroundColorB = value.b;
                BackgroundColorA = value.a;
            }
        }

        public Vector3 ChatPosition
        {
            get
            {
                return new Vector3(PositionX, PositionY, PositionZ);
            }
            set
            {
                PositionX = value.x;
                PositionY = value.y;
                PositionZ = value.z;
            }
        }

        public Vector3 ChatRotation
        {
            get { return new Vector3(RotationX, RotationY, RotationZ); }
            set
            {
                RotationX = value.x;
                RotationY = value.y;
                RotationZ = value.z;
            }
        }

        public ChatConfig()
        {
            _configWatcher = new FileSystemWatcher();
            Task.Run(() =>
            {
                while (!Directory.Exists(Path.GetDirectoryName(FilePath)))
                    Thread.Sleep(100);

                Plugin.Log("FilePath exists! Continuing initialization!");

                string oldFilePath = Path.Combine(Environment.CurrentDirectory, "UserData", "EnhancedTwitchChat.ini");
                string newerFilePath = Path.Combine(Globals.DataPath, "EnhancedTwitchChat.ini");
                if (File.Exists(newerFilePath))
                {
                    // Append the data to the blacklist, if any blacklist info exists, then dispose of the old config file.
                    AppendToBlacklist(newerFilePath);
                    if (!File.Exists(FilePath))
                        File.Move(newerFilePath, FilePath);
                    else
                        File.Delete(newerFilePath);
                }
                else if (File.Exists(oldFilePath))
                {
                    // Append the data to the blacklist, if any blacklist info exists, then dispose of the old config file.
                    AppendToBlacklist(oldFilePath);
                    if (!File.Exists(FilePath))
                        File.Move(oldFilePath, FilePath);
                    else
                        File.Delete(oldFilePath);
                }

                if (File.Exists(FilePath))
                {
                    Load();

                    var text = File.ReadAllText(FilePath);
                    if (text.Contains("TwitchUsername="))
                    {
                        SemiOldConfigOptions semiOldConfigInfo = new SemiOldConfigOptions();
                        ObjectSerializer.Load(semiOldConfigInfo, FilePath);

                        TwitchLoginConfig.Instance.TwitchChannelName = semiOldConfigInfo.TwitchChannelName;
                        TwitchLoginConfig.Instance.TwitchUsername = semiOldConfigInfo.TwitchUsername;
                        TwitchLoginConfig.Instance.TwitchOAuthToken = semiOldConfigInfo.TwitchOAuthToken;
                        TwitchLoginConfig.Instance.Save(true);
                    }
                }
                Save();

                _configWatcher.Path = Path.GetDirectoryName(FilePath);
                _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
                _configWatcher.Filter = $"{Plugin.ModuleName.Replace(" ", "")}.ini";
                _configWatcher.EnableRaisingEvents = true;

                _configWatcher.Changed += ConfigWatcherOnChanged;
            });
        }

        ~ChatConfig()
        {
            _configWatcher.Changed -= ConfigWatcherOnChanged;
        }

        public void Load()
        {
            ObjectSerializer.Load(this, FilePath);

            CorrectConfigSettings();
        }

        public void Save(bool callback = false)
        {
            if (!callback)
                _saving = true;

            ObjectSerializer.Save(this, FilePath);
        }

        private void AppendToBlacklist(string path)
        {
            string text = File.ReadAllText(path);

            if (text.Contains("SongBlacklist="))
            {
                var oldConfig = new OldBlacklistOption();
                ObjectSerializer.Load(oldConfig, path);

                if (oldConfig.SongBlacklist.Length > 0)
                    File.AppendAllText(Path.Combine(Globals.DataPath, "SongBlacklistMigration.list"), oldConfig.SongBlacklist + ",");
            }
        }
        
        private void CorrectConfigSettings()
        {
            if (BackgroundPadding < 0)
                BackgroundPadding = 0;
            if (MaxChatLines < 1)
                MaxChatLines = 1;
        }

        private void ConfigWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_saving)
            {
                _saving = false;
                return;
            }

            Load();
            ConfigChangedEvent?.Invoke(this);
        }
    }
}