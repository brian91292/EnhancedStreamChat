using CustomUI.BeatSaber;
using StreamCore.Chat;
using StreamCore.Config;
using IllusionPlugin;
using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamCore
{
    public class Plugin : IPlugin
    {
        public static readonly string ModuleName = "StreamCore";

        public string Name => ModuleName;
        public string Version => "1.0.0";

        public static Plugin Instance { get; private set; }
        
        private readonly TwitchLoginConfig TwitchLoginConfig = new TwitchLoginConfig();

        public static void Log(string text,
                        [CallerFilePath] string file = "",
                        [CallerMemberName] string member = "",
                        [CallerLineNumber] int line = 0)
        {
            Console.WriteLine($"[{ModuleName}] {DateTime.UtcNow} {Path.GetFileName(file)}->{member}({line}): {text}");
        }

        private IEnumerator DelayedStartup()
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "MenuCore");
            if (TwitchLoginConfig.Instance.TwitchChannelName == String.Empty)
                yield return new WaitUntil(() => BeatSaberUI.DisplayKeyboard("Enter Your Twitch Channel Name!", String.Empty, null, (channelName) => { TwitchLoginConfig.Instance.TwitchChannelName = channelName; TwitchLoginConfig.Instance.Save(true); }));
        }

        public void OnApplicationStart()
        {
            if (Instance != null) return;
            Instance = this;

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            
            SharedCoroutineStarter.instance.StartCoroutine(DelayedStartup());
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            if (to.name == "MenuCore")
                Globals.IsAtMainMenu = true;
            else if (to.name == "GameCore")
                Globals.IsAtMainMenu = false;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

            Globals.IsApplicationExiting = true;

            TwitchWebSocketClient.Shutdown();
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
