﻿using BeatSaverDownloader.Misc;
using IllusionPlugin;
using Microsoft.Win32;
using SimpleJSON;
using SongBrowserPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using UnityEngine;

namespace BeatSaverDownloader
{
    class PluginConfig
    {
        static public List<Playlist> playlists = new List<Playlist>();

        static private bool beatDropInstalled = false;
        static private string beatDropInstallLocation = "";

        static private string configPath = "favoriteSongs.cfg";
        static private string songBrowserSettings = "song_browser_settings.xml";

        public static List<string> favoriteSongs = new List<string>();

        public static string beatsaverURL = "https://beatsaver.com";
        public static string apiAccessToken { get; private set; }

        static public string apiTokenPlaceholder = "replace-this-with-your-api-token";

        public static bool disableSongListTweaks = false;
        public static bool disableDeleteButton = false;

        public static void LoadOrCreateConfig()
        {
            if (!Directory.Exists("UserData"))
            {
                Directory.CreateDirectory("UserData");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "beatsaverURL"))
            {
                ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                Logger.StaticLog("Created config");
            }
            else
            {
                beatsaverURL = ModPrefs.GetString("BeatSaverDownloader", "beatsaverURL");
                if (string.IsNullOrEmpty(beatsaverURL))
                {
                    ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                    beatsaverURL = "https://beatsaver.com";
                    Logger.StaticLog("Created config");
                }
                else
                {
                    Logger.StaticLog("Loaded config");
                }
            }
            
            if (!ModPrefs.HasKey("BeatSaverDownloader", "apiAccessToken"))
            {
                if (!AskForAccessToken())
                {
                    ModPrefs.SetString("BeatSaverDownloader", "apiAccessToken", apiTokenPlaceholder);
                }
            }
            else
            {
                apiAccessToken = ModPrefs.GetString("BeatSaverDownloader", "apiAccessToken");
                if (string.IsNullOrEmpty(apiAccessToken) || apiAccessToken == apiTokenPlaceholder)
                {
                    if (!AskForAccessToken())
                    {
                        ModPrefs.SetString("BeatSaverDownloader", "apiAccessToken", apiTokenPlaceholder);
                        apiAccessToken = apiTokenPlaceholder;
                    }
                }
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "disableDeleteButton"))
            {
                ModPrefs.SetBool("BeatSaverDownloader", "disableDeleteButton", false);
                Logger.StaticLog("Created config");
            }
            else
            {
                disableDeleteButton = ModPrefs.GetBool("BeatSaverDownloader", "disableDeleteButton", false, true);
            }
            
            if (!File.Exists(configPath))
            {
                File.Create(configPath);
            }

            favoriteSongs.AddRange(File.ReadAllLines(configPath, Encoding.UTF8));

            if(IllusionInjector.PluginManager.Plugins.Count(x => x.Name == "Song Browser") > 0)
            {
                Logger.StaticLog("Song Browser installed, disabling Song List Tweaks");
                disableSongListTweaks = true;
                return;
            }

            try
            {
                if (Registry.CurrentUser.OpenSubKey(@"Software").GetSubKeyNames().Contains("178eef3d-4cea-5a1b-bfd0-07a21d068990"))
                {
                    beatDropInstallLocation = (string)Registry.CurrentUser.OpenSubKey(@"Software\178eef3d-4cea-5a1b-bfd0-07a21d068990").GetValue("InstallLocation", "");
                    if (Directory.Exists(beatDropInstallLocation))
                    {
                        beatDropInstalled = true;
                    }
                    else if (Directory.Exists("%LocalAppData%\\Programs\\BeatDrop\\playlists"))
                    {
                        beatDropInstalled = true;
                        beatDropInstallLocation = "%LocalAppData%\\Programs\\BeatDrop\\playlists";
                    }
                    else
                    {
                        beatDropInstalled = false;
                        beatDropInstallLocation = "";                        
                    }
                }
            }
            catch (Exception e)
            {
                Logger.StaticLog($"Can't open registry key! Exception: {e}");
                if (Directory.Exists("%LocalAppData%\\Programs\\BeatDrop\\playlists"))
                {
                    beatDropInstalled = true;
                    beatDropInstallLocation = "%LocalAppData%\\Programs\\BeatDrop\\playlists";
                }
                else
                {
                    Logger.StaticLog("Can't find the BeatDrop installation folder!");
                }
            }

            if (!Directory.Exists("Playlists"))
            {
                Directory.CreateDirectory("Playlists");
            }

            LoadPlaylists();
            LoadSongBrowserConfig();
        }
        
        public static bool AskForAccessToken()
        {
            if (ModPrefs.GetBool("BeatSaverDownloader", "doNotRemindAboutAccessToken", false, true))
                return false;

            switch (MessageBox.Show("Would you like to set an access token for BeatSaver.com?\n(Cancel - don't remind me again)", "No Access Token", MessageBoxButtons.YesNoCancel))
            {
                case DialogResult.Yes:
                    {
                        Process.Start($"{beatsaverURL}/profile/token");

                        InputBox.SetLanguage(InputBox.Language.English);
                        if(InputBox.ShowDialog("Create a read/write token on the opened page and place it below:", "No Access Token", buttons: InputBox.Buttons.OkCancel, type: InputBox.Type.TextBox, ShowInTaskBar: true) == DialogResult.OK)
                        {
                            apiAccessToken = InputBox.ResultValue;
                            if (string.IsNullOrEmpty(apiAccessToken))
                            {
                                return false;
                            }
                            else
                            {
                                ModPrefs.SetString("BeatSaverDownloader", "apiAccessToken", apiAccessToken);
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case DialogResult.Cancel:
                    {
                        ModPrefs.SetBool("BeatSaverDownloader", "doNotRemindAboutAccessToken", true);
                        return false;
                    }
                case DialogResult.No:
                default:
                    {
                        return false;
                    }
            }
        }

        public static void LoadSongBrowserConfig()
        {
            if (!File.Exists(songBrowserSettings))
            {
                return;
            }

            FileStream fs = null;
            try
            {
                fs = File.OpenRead(songBrowserSettings);

                XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));

                SongBrowserSettings settings = (SongBrowserSettings)serializer.Deserialize(fs);

                favoriteSongs.AddRange(settings.favorites);

                fs.Close();

                SaveConfig();
            }
            catch (Exception e)
            {
                Logger.StaticLog($"Can't parse BeatSaberSongBrowser settings file! Exception: {e}");
                if (fs != null) { fs.Close(); }
            }
        }

        public static void LoadPlaylists()
        {
            try
            {
                List<string> playlistFiles = new List<string>();
                if (beatDropInstalled)
                {
                    string[] beatDropPlaylists = Directory.GetFiles(Path.Combine(beatDropInstallLocation, "playlists"), "*.json");
                    playlistFiles.AddRange(beatDropPlaylists);
                    Logger.StaticLog($"Found {beatDropPlaylists.Length} playlists in BeatDrop folder");
                }
                string[] localPlaylists = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Playlists"), "*.json");
                playlistFiles.AddRange(localPlaylists);
                Logger.StaticLog($"Found {localPlaylists.Length} playlists in Playlists folder");
                
                foreach(string path in playlistFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        JSONNode playlistNode = JSON.Parse(json);

                        Playlist playlist = new Playlist();
                        string image = playlistNode["image"].Value;
                        playlist.icon = PluginUI.PluginUI.Base64ToSprite(image.Substring(image.IndexOf(",")+1));
                        playlist.playlistTitle = playlistNode["playlistTitle"];
                        playlist.playlistAuthor = playlistNode["playlistAuthor"];
                        playlist.songs = new List<PlaylistSong>();

                        foreach (JSONNode node in playlistNode["songs"].AsArray)
                        {
                            PlaylistSong song = new PlaylistSong();
                            song.key = node["key"];
                            song.songName = node["songName"];

                            playlist.songs.Add(song);
                        }

                        playlist.fileLoc = path;

                        playlists.Add(playlist);
                        Logger.StaticLog($"Found \"{playlist.playlistTitle}\" by {playlist.playlistAuthor}");
                    } catch (Exception e)
                    {
                        Logger.StaticLog($"Can't parse playlist at {path}! Exception: {e}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.StaticLog("Can't load playlists! Exception: " + e);
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllLines(configPath, favoriteSongs.Distinct().ToArray(), Encoding.UTF8);
        }

    }
}
