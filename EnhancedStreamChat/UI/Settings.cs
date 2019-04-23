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
using CustomUI.Settings;
using CustomUI.BeatSaber;
using CustomUI.MenuButton;

namespace EnhancedStreamChat.UI
{
    public class Settings
    {
        private static float[] incrementValues(float startValue = 0.0f, float step = 0.1f, int numberOfElements = 11)
        {
            if (step < 0.01f)
            {
                throw new Exception("Step value specified was too small! Minimum supported step value is 0.01");
            }
            Int64 multiplier = 100;
            // Avoid floating point math as it results in rounding errors
            Int64 fixedStart = (Int64)(startValue * multiplier);
            Int64 fixedStep = (Int64)(step * multiplier);
            var values = new float[numberOfElements];
            for (int i = 0; i < values.Length; i++)
                values[i] = (float)(fixedStart + (fixedStep * i)) / multiplier;
            return values;
        }

        public static void OnLoad()
        {
            //var reconnectButton = MenuButtonUI.AddButton("Reconnect to Twitch", "Click this button if your twitch chat stops working, and hopefully with some luck it will fix it.", () =>
            //{
            //    Task.Run(() => TwitchWebSocketClient.Connect(true));
            //});

            //MenuButtonUI.AddButton("Restart Game", () => { System.Diagnostics.Process.Start(Path.Combine(Environment.CurrentDirectory, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), Environment.CommandLine); Application.Quit(); });

            // Main menu
            var menu = SettingsUI.CreateSubMenu("Enhanced Stream Chat");
            var channelName = menu.AddString("Twitch Channel Name", "The name of the channel you want Enhanced Twitch Chat to monitor");
            channelName.SetValue += (channel) => { TwitchLoginConfig.Instance.TwitchChannelName = channel; };
            channelName.GetValue += () => { return TwitchLoginConfig.Instance.TwitchChannelName; };
            
            // Chat Filters menu
            var filtersMenu = menu.AddSubMenu("Filters", "Enable or disable various chat filters to keep your chat tidy.", true);

            var clearChatEnabled = filtersMenu.AddBool("Clear Chat", "When enabled, the in-game chat will be cleared if a moderator runs the /clear command.");
            clearChatEnabled.SetValue += (clear) => { ChatConfig.Instance.ClearChatEnabled = clear; };
            clearChatEnabled.GetValue += () => { return ChatConfig.Instance.ClearChatEnabled; };

            var clearTimedOutMessages = filtersMenu.AddBool("Timed Out Messages", "When enabled, timed out messages will be removed from the in-game chat.");
            clearTimedOutMessages.SetValue += (clear) => { ChatConfig.Instance.ClearTimedOutMessages = clear; };
            clearTimedOutMessages.GetValue += () => { return ChatConfig.Instance.ClearTimedOutMessages; };

            var commandMessages = filtersMenu.AddBool("Command Messages", "When enabled, messages starting with ! will not be displayed in the in-game chat.");
            commandMessages.SetValue += (filter) => { ChatConfig.Instance.FilterCommandMessages = filter; };
            commandMessages.GetValue += () => { return ChatConfig.Instance.FilterCommandMessages; };

            var broadcasterMessages = filtersMenu.AddBool("Broadcaster Messages", "When enabled, messages from the broadcaster account will not be displayed in the in-game chat.");
            broadcasterMessages.SetValue += (filter) => { ChatConfig.Instance.FilterBroadcasterMessages = filter; };
            broadcasterMessages.GetValue += () => { return ChatConfig.Instance.FilterBroadcasterMessages; };

            var selfMessages = filtersMenu.AddBool("Self Messages", "When enabled, messages from the logged in Twitch account will not be displayed in the in-game chat.");
            selfMessages.SetValue += (filter) => { ChatConfig.Instance.FilterSelfMessages = filter; };
            selfMessages.GetValue += () => { return ChatConfig.Instance.FilterSelfMessages; };
            
            // Preferences menu
            var prefsMenu = menu.AddSubMenu("Preferences", "Enable or disable specific chat features.", true);

            var animatedEmotes = prefsMenu.AddBool("Animated Emotes", "Enables animated BetterTwitchTV/FrankerFaceZ/Cheermotes in the chat. When disabled, these emotes will still appear but will not be animated.");
            animatedEmotes.SetValue += (animted) => { ChatConfig.Instance.AnimatedEmotes = animted; };
            animatedEmotes.GetValue += () => { return ChatConfig.Instance.AnimatedEmotes; };

            var drawShadows = prefsMenu.AddBool("Draw Shadows", "When enabled, shadows will be drawn behind the text and emotes/badges.");
            drawShadows.SetValue += (shadows) => { ChatConfig.Instance.DrawShadows = shadows; };
            drawShadows.GetValue += () => { return ChatConfig.Instance.DrawShadows; };

            var lockChatPosition = prefsMenu.AddBool("Lock Chat Position", "When enabled, the chat cannot be moved by pointing your controller at it and holding the trigger.");
            lockChatPosition.SetValue += (locked) => { ChatConfig.Instance.LockChatPosition = locked; };
            lockChatPosition.GetValue += () => { return ChatConfig.Instance.LockChatPosition; };

            var reverseChatOrder = prefsMenu.AddBool("Reverse Chat Order", "Makes the chat scroll from top to bottom instead of bottom to top.");
            reverseChatOrder.SetValue += (order) => { ChatConfig.Instance.ReverseChatOrder = order; };
            reverseChatOrder.GetValue += () => { return ChatConfig.Instance.ReverseChatOrder; };

            var showBttvEmotes = prefsMenu.AddBool("Show BTTV Emotes", "When enabled, emotes from BetterTwitchTV will be displayed in chat.");
            showBttvEmotes.SetValue += (bttv) => { ChatConfig.Instance.ShowBTTVEmotes = bttv; };
            showBttvEmotes.GetValue += () => { return ChatConfig.Instance.ShowBTTVEmotes; };
            
            var showFfzEmotes = prefsMenu.AddBool("Show FFZ Emotes", "When enabled, emotes from FrankerFaceZ will be displayed in chat.");
            showFfzEmotes.SetValue += (ffz) => { ChatConfig.Instance.ShowFFZEmotes = ffz; };
            showFfzEmotes.GetValue += () => { return ChatConfig.Instance.ShowFFZEmotes; };

            // UI Settings menu
            var uiMenu = menu.AddSubMenu("User Interface", "All settings related to the chat user interface.", true);

            var fontName = uiMenu.AddString("Menu Font Name", "The name of the system font you want to use for the chat. This can be any font you've installed on your computer!");
            fontName.SetValue += (font) => { ChatConfig.Instance.FontName = font; };
            fontName.GetValue += () => { return ChatConfig.Instance.FontName; };

            var chatScale = uiMenu.AddSlider("Chat Scale", "The size of text and emotes in the chat.", 0f, 10f, 0.1f, false);
            chatScale.SetValue += (scale) => { ChatConfig.Instance.ChatScale = scale; };
            chatScale.GetValue += () => { return ChatConfig.Instance.ChatScale; };

            var chatWidth = uiMenu.AddSlider("Chat Width", "The width of the chat.", 0, 1000, 1, true);
            chatWidth.SetValue += (width) => { ChatConfig.Instance.ChatWidth = width; };
            chatWidth.GetValue += () => { return (int)ChatConfig.Instance.ChatWidth; };

            var messageSpacing = uiMenu.AddSlider("Line Spacing", "The amount of vertical space between each line in chat.", 0, 20, 2, true);
            messageSpacing.SetValue += (spacing) => { ChatConfig.Instance.LineSpacing = spacing; };
            messageSpacing.GetValue += () => { return (int)ChatConfig.Instance.LineSpacing; };

            var backgroundPadding = uiMenu.AddSlider("Background Padding", "The amount of padding to add between the text and the edge of the chat background.", 0, 20, 1, false);
            backgroundPadding.SetValue += (padding) => { ChatConfig.Instance.BackgroundPadding = padding; };
            backgroundPadding.GetValue += () => { return ChatConfig.Instance.BackgroundPadding; };

            var textColor = uiMenu.AddColorPicker("Text Color", "Choose the color of the menu text.", ChatConfig.Instance.TextColor);
            textColor.SetValue += (texCol) => { ChatConfig.Instance.TextColor = texCol; };
            textColor.GetValue += () => { return ChatConfig.Instance.TextColor; };

            var backgroundColor = uiMenu.AddColorPicker("Background Color", "Choose the color of the menu background.", ChatConfig.Instance.BackgroundColor);
            backgroundColor.SetValue += (bgCol) => { ChatConfig.Instance.BackgroundColor = bgCol; };
            backgroundColor.GetValue += () => { return ChatConfig.Instance.BackgroundColor; };
        }
    }
}
