# Mod Info
Enhanced Stream Chat is a rich text stream chat integration mod (currently supports Twitch) with full unicode, emote, cheermote, and emoji support.

# Features
- Full Rich Text Support, see ALL of your Twitch/YouTube chat while immersed in VR!
  - This includes all Twitch badges, emotes, cheermotes, BetterTwitchTV emotes, FrankerFaceZ emotes, all Emojis (provided by [twemoji](https://github.com/twitter/twemoji)) and even animated emotes!
  - On YouTube, the users profile image will be shown alongside their username.
  - This also includes full Unicode Support! This means you can enjoy the chat in any language!

# Dependencies
Enhanced Twitch Chat depends on [BSML](https://github.com/monkeymanboy/BeatSaberMarkupLanguage), and [StreamCore](https://github.com/brian91292/StreamCore/releases). Make sure to install them, or Enhanced Stream Chat won't work!
  
# Installation
Copy EnhancedStreamChat.dll to your Beat Saber\Plugins folder, and install all of its dependencies. That's it!

# Usage
All you need to enter is the channel name which you want to join (see the `Setup` section below), the chat will show up to your right in game, and you can move it by pointing at it with the laser from your controller and grabbing it with the trigger. You can also move the chat towards you and away from you by pressing up and down on the analog stick or trackpad on your controller. Finally, you can use the lock button in the corner of the chat to lock the chat position in place so you don't accidentally move it.

# Setup
Most common options can be configured directly via the Beat Saber settings menu in the Enhanced Stream Chat submenu, as seen in the image below. 
![Enhanced Stream Chat settings menu](https://i.imgur.com/GSPmjPb.jpg)

# Config
To manually edit the config options, browse to `Beat Saber\UserData\StreamCore\EnhancedStreamChat.ini` file.  *Keep in mind all config options will update in realtime when you save the file! This means you don't have to restart the game to see your changes!* Use the tables below as a guide for setting these values:

### EnhancedStreamChat.ini
| Option | Description |
| - | - |
| **FontName** | The name of the system font that should be used for chat messages. You can specify any font installed on your computer! |
| **ChatScale** | How large the chat messages/emotes should be displayed. |
| **ChatWidth** | The width of the chat, regardless of ChatScale. |
| **LineSpacing** | Determines the amount of extra spacing between lines of chat |
| **MaxChatLines** | The maximum number of lines allowed in chat at once. |
| **PositionX/Y/Z** | The location of the chat in game (this can be adjusted in game, see description above!) |
| **RotationX/Y/Z** | The rotation of the chat in game (this can be adjusted in game, see description above!) |
| **TextColorR/G/B/A** | The color of chat messages, on a scale of 0-1. If your colors are between 0-255, just divide by 255 to get this value! |
| **BackgroundColorR/G/B/A** | The color of the chat background, on a scale of 0-1. If your colors are between 0-255, just divide by 255 to get this value! |
| **BackgroundPadding** | Determines how much empty space there will be around the borders of the chat. |
| **AnimatedEmotes** | When set to false, animated emotes/cheermotes will not move at all. |
| **DrawShadows** | When set to true, shadows will be drawn behind emotes/text (looks nicer in windowed view, not really noticeable in headset). |
| **LockChatPosition** | Whether or not the chat can be moved by pointing at it with the controller laser and gripping with the trigger. |
| **ReverseChatOrder** | When set to true, chat messages will enter from the top and exit on bottom instead of entering on bottom and exiting on top. |
| **ShowBTTVEmotes** | When set to true, BetterTwitchTV emotes will be shown in chat. |
| **ShowFFZEmotes** | When set to true, FrankerFaceZ emotes will be shown in chat. |
| **FilterCommandMessages** | When set to true, messages starting with ! won't be shown in chat. |
| **FilterBroadcasterMessages** | When set to true, messages from the broadcaster won't be shown in chat. |
| **FilterSelfMessages** | When set to true, the logged in account will not be shown in chat. |

# Compiling
To compile this mod simply clone the repo and update the project references to reference the corresponding assemblies in the `Beat Saber\Beat Saber_Data\Managed` folder, then compile. You may need to remove the post build event if your Beat Saber directory isn't at the same location as mine.

# Download
[Click here to download the latest EnhancedStreamChat.dll](https://github.com/brian91292/EnhancedStreamChat/releases)
