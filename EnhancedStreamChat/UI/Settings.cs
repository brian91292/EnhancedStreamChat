using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using EnhancedStreamChat.Chat;
using EnhancedStreamChat.Config;
//using EnhancedTwitchIntegration.Config;
using System.IO;
using StreamCore.Config;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using StreamCore;
//using CustomUI.Settings;
//using CustomUI.BeatSaber;
//using CustomUI.MenuButton;

namespace EnhancedStreamChat.Config
{
    public partial class ChatConfig : PersistentSingleton<ChatConfig> //BSMLViewController, INotifiableHost
    {
        public string ResourceName => "EnhancedStreamChat.Resources.Settings.bsml";

        //public override string ContentFilePath => @"C:\Users\brian\Documents\GitHub\BeatSaber-EnhancedTwitchChat\EnhancedStreamChat\Resources\Settings.bsml";

        [UIValue("twitch-channel-name")]
        public string TwitchChannelName
        {
            get => TwitchLoginConfig.Instance?.TwitchChannelName;
            set
            {
                if (TwitchLoginConfig.Instance != null) {
                    TwitchLoginConfig.Instance.TwitchChannelName = value;
                    TwitchLoginConfig.Instance.Save(true);
                }
            }
        }

        [UIAction("#apply")]
        private void Apply()
        {
            Plugin.Log("Saving config!");
            this.Save(true);
        }

        //private static IEnumerator PresentTest()
        //{
        //    yield return new WaitForSeconds(1);
        //    Settings testViewController = BeatSaberMarkupLanguage.BeatSaberUI.CreateViewController<Settings>();
        //    Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First().InvokeMethod("PresentViewController", new object[] { testViewController, null, false });
        //}

        public static void OnLoad()
        {
            //SharedMonoBehaviour.StartCoroutine(PresentTest());
            BSMLSettings.instance.AddSettingsMenu("<size=3.3>Enhanced Stream Chat</size>", "EnhancedStreamChat.Resources.Settings.bsml", PersistentSingleton<ChatConfig>.instance);
        }
    }
}
