using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WelcomeMusic", "Ajie", "1.0.9")]
    [Description("Play music for players when they join the server.")]
    class WelcomeMusic : RustPlugin
    {
        List<BasePlayer> musicPlayers = new List<BasePlayer>();
        private ConfigData configData;
        private Dictionary<BasePlayer, BaseEntity> BoomBoxList = new Dictionary<BasePlayer, BaseEntity>();
        private JoinData joinData;
        private string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";
        private string BoomboxPrefab = "assets/prefabs/voiceaudio/boombox/boombox.deployed.prefab";
        class ConfigData
        {
            [JsonProperty("Permission Name")]
            public string PermissionName = "welcomemusic.use";
            [JsonProperty("Need Permission")]
            public bool NeedPermission = true;
            [JsonProperty("Music URL")]
            public string URL = "https://yourwebsite.com/music.mp3";
            [JsonProperty("Use Random List")]
            public bool UseList = false;
            [JsonProperty("Random Music URL List (URL | Duration)")]
            public Dictionary<string, float> URLList = new Dictionary<string, float>();
            [JsonProperty("Music Duration (sec)")]
            public float Duration = 15f;
            [JsonProperty("Music Delay (sec)")]
            public float Delay = 5f;
            [JsonProperty("Only first-time join the server")]
            public bool OnlyFrist = false;
            [JsonProperty("(First-time) Music URL (Empty = none)")]
            public string URLF = "";
            [JsonProperty("(First-time) Music Duration (sec)")]
            public float DurationF = 15f;
            [JsonProperty("Players can disable the WelcomeMusic (/wm)")]
            public bool CanDisable = false;
            [JsonProperty("Welcome Message (Empty = No Message)")]
            public string Message = "Welcome To Our Server, Now playing music for you~ (You can use command /wm to disable)";
        }
        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            ValidateConfig();
            SaveConfig(configData);
            return true;
        }
        void ValidateConfig()
        {
            if (configData.URLList.Count <= 0 || configData.URLList == null)
            {
                configData.URLList = new Dictionary<string, float>
                {
                    ["https://github.com/blgarust/music/raw/main/NeverGonnaGiveYouUp.mp3"] = 30f,
                    ["https://github.com/blgarust/music/raw/main/WelcomeToOurServer.mp3"] = 5f,
                };
            }
        }
        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
            if (!string.IsNullOrEmpty(configData.PermissionName))
                permission.RegisterPermission(configData.PermissionName, this);
            permission.RegisterPermission("welcomemusic.toplayer", this);
            if (string.IsNullOrEmpty(configData.URL) && !configData.UseList)
            {
                Puts("You haven't set the Music URL!!!");
            }
            if (configData.UseList && configData.URLList.Count == 0)
            {
                Puts("You haven't set the Music URL List!!!");
            }
            joinData = Interface.Oxide.DataFileSystem.ReadObject<JoinData>(Name);
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            configData = new ConfigData();
            SaveConfig(configData);
        }
        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        public class JoinData
        {
            [JsonProperty(PropertyName = "Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Players = new List<ulong>();
            [JsonProperty(PropertyName = "Disable Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> DisablePlayers = new List<ulong>();
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, joinData);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            if (configData.NeedPermission && !permission.UserHasPermission(player.UserIDString, configData.PermissionName)) return;
            if (BoomBoxList.ContainsKey(player)) return;
            if (configData.CanDisable)
            {
                if (joinData.DisablePlayers.Contains(player.userID)) return;
            }
            if (string.IsNullOrEmpty(configData.URL) && !configData.UseList)
            {
                Puts("You haven't set the Music URL!!!");
                return;
            }
            if (configData.UseList && configData.URLList.Count == 0)
            {
                Puts("You haven't set the Music URL List!!!");
                return;
            }
            if (configData.OnlyFrist)
            {
                if (joinData.Players.Contains(player.userID))
                    return;
                else
                    joinData.Players.Add(player.userID);
            }
            string URL;
            float TIME;
            if (!string.IsNullOrEmpty(configData.URLF) && !joinData.Players.Contains(player.userID))
            {
                URL = configData.URLF;
                TIME = configData.DurationF;
                joinData.Players.Add(player.userID);
            }
            else if (!configData.UseList)
            {
                URL = configData.URL;
                TIME = configData.Duration;
            }
            else
            {
                var r = new System.Random();
                var RMusic = configData.URLList.ElementAt(r.Next(0, configData.URLList.Count));
                URL = RMusic.Key;
                TIME = RMusic.Value;
            }
            timer.Once(configData.Delay + 3f, () => {
                MusicToPlayer(player, URL, TIME);
                if (!string.IsNullOrEmpty(configData.Message))
                    player.ChatMessage(configData.Message);
            });
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (BoomBoxList.ContainsKey(player))
            {
                BaseEntity sph;
                if (BoomBoxList.TryGetValue(player, out sph))
                {
                    BoomBoxList.Remove(player);
                    sph.Kill();
                }
            }
        }
        [ChatCommand("testwm")]
        void TestWelcomeMusicCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage($"[WelcomeMusic] You not have permission to use this command.");
                return;
            }
            if (BoomBoxList.ContainsKey(player))
            {
                player.ChatMessage("[WelcomeMusic] Currently playing music for you, please try again later");
                return;
            }
            if (configData.OnlyFrist && joinData.Players.Contains(player.userID))
            {
                player.ChatMessage("[WelcomeMusic] You have already played the music.");
                return;
            }
            if (joinData.DisablePlayers.Contains(player.userID))
            {
                player.ChatMessage("[WelcomeMusic] You have disabled the welcome music, use command /wm to enable.");
                return;
            }
            OnPlayerConnected(player);
        }
        [ChatCommand("wm")]
        void WelcomeMusicCommand(BasePlayer player, string command, string[] args)
        {
            if (!configData.CanDisable)
            {
                player.ChatMessage($"[WelcomeMusic] The server is not allowed to disable welcome music!");
                return;
            }
            if (!joinData.DisablePlayers.Contains(player.userID))
            {
                joinData.DisablePlayers.Add(player.userID);
                player.ChatMessage($"[WelcomeMusic] You have successfully disabled the welcome music!");
                return;
            }
            else
            {
                joinData.DisablePlayers.Remove(player.userID);
                player.ChatMessage($"[WelcomeMusic] You have successfully enabled the welcome music!");
                return;
            }
        }
        [ChatCommand("musicto")]
        void MusicToCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "welcomemusic.toplayer"))
            {
                player.ChatMessage($"[WelcomeMusic] You not have permission to use this command.");
                return;
            }
            if (args.Length < 3)
            {
                player.ChatMessage($"[WelcomeMusic] Command Usage: /musicto <PlayerID> <MusicURL> <MusicDuration>!");
                return;
            }
            var target = BasePlayer.Find(args[0]);
            if (target == null)
            {
                player.ChatMessage("[WelcomeMusic] No player found.");
                return;
            }
            if (BoomBoxList.ContainsKey(target))
            {
                player.ChatMessage("[WelcomeMusic] Currently playing music for target player, please try again later");
                return;
            }
            if (!args[1].Contains("http"))
            {
                player.ChatMessage("[WelcomeMusic] Please enter a valid URL.");
                return;
            }
            float duration;
            if (!float.TryParse(args[2], out duration))
            {
                player.ChatMessage("[WelcomeMusic] Please enter a valid number.");
                return;
            }
            else
            {
                if (BoomBoxList.ContainsKey(player))
                {
                    var boomBox = BoomBoxList[player];
                    if (boomBox.audioSource.isPlaying)
                    {
                        boomBox.audioSource.Stop();
                    }
                    BoomBoxList.Remove(player);
                }
                MusicToPlayer(target, args[1], duration);
                player.ChatMessage($"[WelcomeMusic] Now Playing for Player {target.displayName}: \nMusicURL: {args[1]}\nMusicDuration: {duration}.");
            }
        }
        [ChatCommand("musicall")]
        void MusicToAllCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "welcomemusic.toplayer"))
            {
                player.ChatMessage($"[WelcomeMusic] You not have permission to use this command.");
                return;
            }
            if (args.Length < 2)
            {
                player.ChatMessage($"[WelcomeMusic] Command Usage: /musicall <MusicURL> <MusicDuration>!");
                return;
            }
            if (!args[0].Contains("http"))
            {
                player.ChatMessage("[WelcomeMusic] Please enter a valid URL.");
                return;
            }
            float duration;
            if (!float.TryParse(args[1], out duration))
            {
                player.ChatMessage("[WelcomeMusic] Please enter a valid number.");
                return;
            }
            else
            {
                foreach (var item in BasePlayer.activePlayerList)
                {
                    MusicToPlayer(item, args[0], duration);
                    System.Threading.Thread.Sleep((int)(duration * 1000)); // wait for the music to finish playing
                }
                player.ChatMessage($"[WelcomeMusic] Now Playing for all player: \nMusicURL: {args[0]}\nMusicDuration: {duration}.");
            }
        }
        [ConsoleCommand("musicto")]
        void MusicToConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                arg.ReplyWith("You do not have permission to use this command.");
                return;
            }
            if (arg == null || arg.Args?.Length != 3)
            {
                arg.ReplyWith("Command Usage: musicto <PlayerID> <MusicURL> <MusicDuration>");
                return;
            }
        
            var target = BasePlayer.Find(arg.Args[0]);
            if (target == null)
            {
                arg.ReplyWith("No player found.");
                return;
            }
            if (BoomBoxList.ContainsKey(target))
            {
                arg.ReplyWith("Currently playing music for target player, please try again later");
                return;
            }
            if (!arg.Args[1].Contains("http"))
            {
                arg.ReplyWith("Please enter a valid URL.");
                return;
            }
            float duration;
            if (!float.TryParse(arg.Args[2], out duration))
            {
                arg.ReplyWith("Please enter a valid number.");
                return;
            }
            else
            {
                MusicToPlayer(target, arg.Args[1], duration);
                arg.ReplyWith($"Now playing for player {target.displayName}: \nMusicURL: {arg.Args[1]}\nMusicDuration: {duration}.");
            }
        }
        public void StopMusicPlayer(BasePlayer player)
        {
            if (player == null)
                return;

            AudioSource source;
            if (musicPlayers.TryGetValue(player.userID, out source))
            {
                source.Stop();
                musicPlayers.Remove(player.userID);
                Destroy(source);
            }
        }

        [ConsoleCommand("musicall")]
        void MusicToAllConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args?.Length != 2)
            {
                Puts("Command Usage: musicall <MusicURL> <MusicDuration>");
                return;
            }
            var argplayer = arg.Player();
            if (arg.Connection != null)
                if (!argplayer.IsAdmin)
                    return;
            if (!arg.Args[0].Contains("http"))
            {
                Puts("Please enter a valid URL.");
                return;
            }
            float duration;
            if (!float.TryParse(arg.Args[1], out duration))
            {
                Puts("Please enter a valid number.");
                return;
            }

            // Stop any previous music
            foreach (var item in BasePlayer.activePlayerList)
            {
                StopMusicPlayer(item);
            }

            foreach (var item in BasePlayer.activePlayerList)
            {
                bool success = MusicToPlayer(item, arg.Args[0], duration);
                if (!success)
                {
                    Puts($"Failed to play music for player {item.displayName}.");
                }
            }
            Puts($"Now Playing for all player: \nMusicURL: {arg.Args[0]}\nMusicDuration: {duration}.");
        }
        private bool MusicToPlayer(BasePlayer player, string url, float duration)
        {
            // Stop previous music player for this player
            StopMusicPlayer(player);
            // Create new music player
            var musicPlayer = StartMusicPlayer(player, url, duration);
            if (musicPlayer == null)
            {
                return false;
            }
            // Add to dictionary
            musicPlayers[player.userID] = musicPlayer;
            return true;
        }
        void Unload()
        {
            SaveData();
            foreach (KeyValuePair<BasePlayer, BaseEntity> BoomBox in BoomBoxList)
            {
                if (BoomBox.Value != null)
                {
                    BoomBox.Value.Kill();
                }
            }
            BoomBoxList.Clear();
        }
    }
}
