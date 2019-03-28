using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using IllusionPlugin;
using EnhancedStreamChat.Chat;
using EnhancedStreamChat.UI;
using System.Threading.Tasks;
using System.Collections;
using CustomUI.BeatSaber;
//using EnhancedTwitchChat.Bot;
using System.Runtime.CompilerServices;
using TMPro;
using EnhancedStreamChat.Config;
using StreamCore.Chat;

namespace EnhancedStreamChat
{
    public class Plugin : IPlugin
    {
        public static readonly string ModuleName = "EnhancedStreamChat";
        public string Name => ModuleName;
        public string Version => "1.2.0-beta6";
        
        public static Plugin Instance { get; private set; }

        private ChatConfig ChatConfig;

        public static void Log(string text,
                        [CallerFilePath] string file = "",
                        [CallerMemberName] string member = "",
                        [CallerLineNumber] int line = 0)
        {
            Console.WriteLine($"[{ModuleName}] {DateTime.UtcNow} {Path.GetFileName(file)}->{member}({line}): {text}");
        }
        
        public void OnApplicationStart()
        {
            if (Instance != null) return;
            Instance = this;
            
            ChatConfig = new ChatConfig();

            TwitchWebSocketClient.Initialize();

            SharedCoroutineStarter.instance.StartCoroutine(DelayedStartup());
        }

        private IEnumerator DelayedStartup()
        {
            yield return new WaitForSeconds(0.5f);

            ChatHandler.OnLoad();

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }
        
        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name == "MenuCore")
            {
                Settings.OnLoad();

                ChatConfig.Save(true);
            }
        }

        public void OnApplicationQuit()
        {
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            try
            {
                ChatHandler.Instance?.SceneManager_activeSceneChanged(from, to);
            }
            catch { }
        }
        
        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnUpdate()
        {
        }
    }
}
