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
using System.Runtime.CompilerServices;
using TMPro;
using EnhancedStreamChat.Config;
using StreamCore.Chat;
using StreamCore.Utils;
using StreamCore.YouTube;
using CustomUI.Utilities;

namespace EnhancedStreamChat
{
    public class Plugin : IPlugin
    {
        public static readonly string ModuleName = "Enhanced Stream Chat";
        public string Name => ModuleName;
        public string Version => "2.1.0";
        
        public static Plugin Instance { get; private set; }

        private ChatConfig ChatConfig;

        public static void Log(string text,
                [CallerFilePath] string file = "",
                [CallerMemberName] string member = "",
                [CallerLineNumber] int line = 0)
        {
            Console.WriteLine($"{ModuleName}::{Path.GetFileName(file)}->{member}({line}): {text}");
        }

        public void OnApplicationStart()
        {
            if (Instance != null) return;
            Instance = this;
            ChatConfig = new ChatConfig();

            TwitchWebSocketClient.Initialize();
            YouTubeConnection.Initialize();
            
            SharedCoroutineStarter.instance.StartCoroutine(DelayedStartup());
        }

        private IEnumerator DelayedStartup()
        {
            yield return new WaitForSeconds(0.5f);

            try
            {
                if (Utilities.IsModInstalled("EnhancedTwitchChat"))
                {
                    string oldFile = Path.Combine(Environment.CurrentDirectory, "Plugins", "EnhancedTwitchChat.dll.old");
                    if (File.Exists(oldFile))
                    {
                        File.Delete(oldFile);
                    }
                    File.Move(Path.Combine(Environment.CurrentDirectory, "Plugins", "EnhancedTwitchChat.dll"), oldFile);
                }
            }
            catch(Exception ex)
            {
                Log(ex.ToString());
            }

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
