using StreamCore.SimpleJSON;
using StreamCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore.Config
{
    public class TwitchLoginConfig
    {
        private string FilePath = Path.Combine(Globals.DataPath, $"TwitchLoginInfo.ini");

        public string TwitchChannelName = "";
        public string TwitchUsername = "";
        public string TwitchOAuthToken = "";
       
        public event Action<TwitchLoginConfig> ConfigChangedEvent;

        private readonly FileSystemWatcher _configWatcher;
        private bool _saving;

        private static TwitchLoginConfig _instance = null;
        public static TwitchLoginConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TwitchLoginConfig();
                return _instance;
            }

            private set
            {
                _instance = value;
            }
        }

        public TwitchLoginConfig()
        {
            Instance = this;

            string oldDataPath = Path.Combine(Environment.CurrentDirectory, "UserData", "EnhancedTwitchChat");
            if (Directory.Exists(oldDataPath) && !Directory.Exists(Globals.DataPath))
                Directory.Move(oldDataPath, Globals.DataPath);

            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            if (File.Exists(FilePath))
            {
                Load();
            }
            CorrectConfigSettings();
            Save();

            _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "TwitchLoginInfo.ini",
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += ConfigWatcherOnChanged;
        }

        ~TwitchLoginConfig()
        {
            _configWatcher.Changed -= ConfigWatcherOnChanged;
        }

        public void Load()
        {
            if(File.Exists(FilePath))
                ConfigSerializer.LoadConfig(this, FilePath);
            else
                ImportAsyncTwitchConfig();

            CorrectConfigSettings();
        }

        public void Save(bool callback = false)
        {
            if (!callback)
                _saving = true;
            ConfigSerializer.SaveConfig(this, FilePath);
        }

        private void ImportAsyncTwitchConfig()
        {
            try
            {
                string asyncTwitchConfig = Path.Combine(Environment.CurrentDirectory, "UserData", "AsyncTwitchConfig.json");
                if (File.Exists(asyncTwitchConfig))
                {
                    JSONNode node = JSON.Parse(File.ReadAllText(asyncTwitchConfig));
                    if (!node.IsNull)
                    {
                        if (node["Username"].IsString && TwitchUsername == String.Empty)
                            TwitchUsername = node["Username"].Value;
                        if (node["ChannelName"].IsString && TwitchChannelName == String.Empty)
                            TwitchChannelName = node["ChannelName"].Value;
                        if (node["OauthKey"].IsString && TwitchOAuthToken == String.Empty)
                            TwitchOAuthToken = node["OauthKey"].Value;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log($"Error when trying to parse AsyncTwitchConfig! {e}");
            }
        }

        private void CorrectConfigSettings()
        {
            if (TwitchOAuthToken != String.Empty && !TwitchOAuthToken.StartsWith("oauth:"))
                TwitchOAuthToken = "oauth:" + TwitchOAuthToken;

            if (TwitchChannelName.Length > 0)
            {
                if (TwitchChannelName.Contains("/"))
                {
                    var tmpChannelName = TwitchChannelName.TrimEnd('/').Split('/').Last();
                    Plugin.Log($"Changing twitch channel to {tmpChannelName}");
                    TwitchChannelName = tmpChannelName;
                    Save();
                }
                TwitchChannelName = TwitchChannelName.ToLower().Replace(" ", "");
            }
        }

        private void ConfigWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_saving)
            {
                _saving = false;
                return;
            }

            Load();

            if (ConfigChangedEvent != null)
            {
                ConfigChangedEvent(this);
            }
        }
    }
}
