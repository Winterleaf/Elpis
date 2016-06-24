/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Elpis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/

using Enumerable = System.Linq.Enumerable;

namespace Elpis
{
    public class HotkeyConfig : HotKey
    {
        private HotkeyConfig(System.Windows.Input.RoutedUICommand c, System.Windows.Input.Key k,
            System.Windows.Input.ModifierKeys m) : base(c, k, m) {}

        public HotkeyConfig(HotKey h)
        {
            Command = h.Command;
            Key = h.Key;
            Modifiers = h.Modifiers;
            Global = h.Global;
            Enabled = h.Enabled;
        }

        public HotkeyConfig(string data, HotkeyConfig def)
        {
            string[] split = data.Split('*');

            bool success = false;
            if (split.Length == 5)
            {
                try
                {
                    Command = PlayerCommands.GetCommandByName(split[0]);
                    Key = (System.Windows.Input.Key) System.Enum.Parse(typeof (System.Windows.Input.Key), split[1]);
                    Modifiers = (System.Windows.Input.ModifierKeys) System.Enum.Parse(typeof (System.Windows.Input.ModifierKeys), split[2]);
                    Global = bool.Parse(split[3]);
                    Enabled = bool.Parse(split[4]);
                    success = true;
                }
                catch
                {
                    //
                }
            }

            if (success) return;
            Key = def.Key;
            Modifiers = def.Modifiers;
            Enabled = def.Enabled;
        }

        public static HotkeyConfig Default => new HotkeyConfig(PlayerCommands.PlayPause, System.Windows.Input.Key.Space,
            System.Windows.Input.ModifierKeys.None);

        public override string ToString()
        {
            return Command.Name + "*" + Key + "*" + Modifiers + "*" + Global + "*" + Enabled;
        }
    }

    public struct ConfigItems
    {
        public static Util.MapConfigEntry Debug_WriteLog = new Util.MapConfigEntry("Debug_WriteLog", false);
        public static Util.MapConfigEntry Debug_Logpath = new Util.MapConfigEntry("Debug_Logpath", Config.ElpisAppData);
        public static Util.MapConfigEntry Debug_Timestamp = new Util.MapConfigEntry("Debug_Timestamp", false);

        public static Util.MapConfigEntry Login_Email = new Util.MapConfigEntry("Login_Email", "");
        public static Util.MapConfigEntry Login_Password = new Util.MapConfigEntry("Login_Password", "");
        public static Util.MapConfigEntry Login_AutoLogin = new Util.MapConfigEntry("Login_AutoLogin", true);

        public static Util.MapConfigEntry Pandora_AudioFormat = new Util.MapConfigEntry("Pandora_AudioFormat",
            PandoraSharp.PAudioFormat.Mp3);

        public static Util.MapConfigEntry Pandora_AutoPlay = new Util.MapConfigEntry("Pandora_AutoPlay", false);
        public static Util.MapConfigEntry Pandora_LastStationID = new Util.MapConfigEntry("Pandora_LastStationID", "");

        public static Util.MapConfigEntry Pandora_StationSortOrder = new Util.MapConfigEntry(
            "Pandora_StationSortOrder", PandoraSharp.Pandora.SortOrder.DateDesc.ToString());

        public static Util.MapConfigEntry Proxy_Address = new Util.MapConfigEntry("Proxy_Address", "");
        public static Util.MapConfigEntry Proxy_Port = new Util.MapConfigEntry("Proxy_Port", 0);
        public static Util.MapConfigEntry Proxy_User = new Util.MapConfigEntry("Proxy_User", "");
        public static Util.MapConfigEntry Proxy_Password = new Util.MapConfigEntry("Proxy_Password", "");

        public static Util.MapConfigEntry Elpis_Version = new Util.MapConfigEntry("Elpis_Version",
            new System.Version().ToString());

        public static Util.MapConfigEntry Elpis_InstallID = new Util.MapConfigEntry("Elpis_InstallID",
            System.Guid.NewGuid().ToString());

        public static Util.MapConfigEntry Elpis_CheckUpdates = new Util.MapConfigEntry("Elpis_CheckUpdates", true);

        public static Util.MapConfigEntry Elpis_CheckBetaUpdates = new Util.MapConfigEntry("Elpis_CheckBetaUpdates",
            false);

        public static Util.MapConfigEntry Elpis_RemoteControlEnabled =
            new Util.MapConfigEntry("Elpis_RemoteControlEnabled", true);

        public static Util.MapConfigEntry Elpis_MinimizeToTray = new Util.MapConfigEntry("Elpis_MinimizeToTray", false);

        public static Util.MapConfigEntry Elpis_ShowTrayNotifications =
            new Util.MapConfigEntry("Elpis_ShowTrayNotifications", true);

        public static Util.MapConfigEntry Elpis_StartupLocation = new Util.MapConfigEntry("Elpis_StartupLocation", "");
        public static Util.MapConfigEntry Elpis_StartupSize = new Util.MapConfigEntry("Elpis_StartupSize", "");
        public static Util.MapConfigEntry Elpis_Volume = new Util.MapConfigEntry("Elpis_Volume", 100);
        public static Util.MapConfigEntry Elpis_PauseOnLock = new Util.MapConfigEntry("Elpis_PauseOnLock", false);
        public static Util.MapConfigEntry Elpis_MaxHistory = new Util.MapConfigEntry("Elpis_MaxHistory", 8);

        public static Util.MapConfigEntry LastFM_Scrobble = new Util.MapConfigEntry("LastFM_Scrobble", false);
        public static Util.MapConfigEntry LastFM_SessionKey = new Util.MapConfigEntry("LastFM_SessionKey", "");

        public static Util.MapConfigEntry HotKeysList = new Util.MapConfigEntry("HotKeysList",
            new System.Collections.Generic.Dictionary<int, string>());

        //public static MapConfigEntry Misc_ForceSSL = new MapConfigEntry("Misc_ForceSSL", false);
        public static Util.MapConfigEntry System_OutputDevice = new Util.MapConfigEntry("System_OutputDevice", "");

        public static Util.MapConfigEntry RipStream = new Util.MapConfigEntry("RipStream", false);
        public static Util.MapConfigEntry RipPath = new Util.MapConfigEntry("RipPath", "");
    }

    public struct ConfigDropDownItem
    {
        public string Display { get; set; }
        public string Value { get; set; }
    }

    public struct ConfigFields
    {
        public bool RipStream { get; set; }
        public string RipPath { get; set; }

        public bool Debug_WriteLog { get; set; }
        public string Debug_Logpath { get; set; }
        public bool Debug_Timestamp { get; set; }

        public string Login_Email { get; set; }
        public string Login_Password { get; set; }
        public bool Login_AutoLogin { get; set; }

        public string Pandora_AudioFormat { get; set; }
        public bool Pandora_AutoPlay { get; set; }
        public string Pandora_LastStationID { get; set; }
        public string Pandora_StationSortOrder { get; set; }

        public string Proxy_Address { get; set; }
        public int Proxy_Port { get; set; }
        public string Proxy_User { get; set; }
        public string Proxy_Password { get; set; }

        public System.Version Elpis_Version { get; internal set; }
        public string Elpis_InstallID { get; internal set; }
        public bool Elpis_CheckUpdates { get; set; }
        public bool Elpis_CheckBetaUpdates { get; set; }
        public bool Elpis_RemoteControlEnabled { get; set; }

        public bool Elpis_MinimizeToTray { get; set; }
        public bool Elpis_ShowTrayNotifications { get; set; }
        public int Elpis_Volume { get; set; }
        public bool Elpis_PauseOnLock { get; set; }
        public int Elpis_MaxHistory { get; set; }

        public bool LastFM_Scrobble { get; set; }
        public string LastFM_SessionKey { get; set; }

        //public bool Misc_ForceSSL { get; set; }

        public System.Windows.Point Elpis_StartupLocation { get; set; }
        public System.Windows.Size Elpis_StartupSize { get; set; }

        public System.Collections.Generic.Dictionary<int, HotkeyConfig> Elpis_HotKeys { get; set; }

        public string System_OutputDevice { get; set; }
    }

    public class Config
    {
        public Config(string configSuffix = "")
        {
            configSuffix = configSuffix.Trim().Replace(" ", "");
            if (configSuffix != "")
            {
                _configFile = "config_" + configSuffix + ".config";
            }

            string appData = (string) ConfigItems.Debug_Logpath.Default;
            if (!System.IO.Directory.Exists(appData))
                System.IO.Directory.CreateDirectory(appData);

            string config = System.IO.Path.Combine(appData, _configFile);

            _c = new Util.MapConfig(config);

            Fields.Elpis_HotKeys = new System.Collections.Generic.Dictionary<int, HotkeyConfig>();

            //If not config file, init with defaults then save
            if (!System.IO.File.Exists(config))
            {
                LoadConfig();
                SaveConfig();
            }
        }

        public static readonly string ElpisAppData =
            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "Elpis");

        private readonly Util.MapConfig _c;
        private readonly string _configFile = "elpis.config";

        public ConfigFields Fields;

        public bool LoadConfig()
        {
            if (!_c.LoadConfig())
                return false;

            Fields.RipStream = (bool) _c.GetValue(ConfigItems.RipStream);
            Fields.RipPath = (string) _c.GetValue(ConfigItems.RipPath);

            Fields.Debug_WriteLog = (bool) _c.GetValue(ConfigItems.Debug_WriteLog);
            Fields.Debug_Logpath = (string) _c.GetValue(ConfigItems.Debug_Logpath);
            Fields.Debug_Timestamp = (bool) _c.GetValue(ConfigItems.Debug_Timestamp);

            Fields.Login_Email = (string) _c.GetValue(ConfigItems.Login_Email);
            Fields.Login_Password = _c.GetEncryptedString(ConfigItems.Login_Password);

            Fields.Login_AutoLogin = (bool) _c.GetValue(ConfigItems.Login_AutoLogin);

            Fields.Pandora_AudioFormat = (string) _c.GetValue(ConfigItems.Pandora_AudioFormat);
            if (Fields.Pandora_AudioFormat != PandoraSharp.PAudioFormat.AacPlus &&
                Fields.Pandora_AudioFormat != PandoraSharp.PAudioFormat.Mp3 &&
                Fields.Pandora_AudioFormat != PandoraSharp.PAudioFormat.Mp3Hifi)
            {
                Fields.Pandora_AudioFormat = PandoraSharp.PAudioFormat.Mp3;
            }
            Fields.Pandora_AutoPlay = (bool) _c.GetValue(ConfigItems.Pandora_AutoPlay);
            Fields.Pandora_LastStationID = (string) _c.GetValue(ConfigItems.Pandora_LastStationID);
            Fields.Pandora_StationSortOrder = (string) _c.GetValue(ConfigItems.Pandora_StationSortOrder);

            Fields.Proxy_Address = ((string) _c.GetValue(ConfigItems.Proxy_Address)).Trim();
            Fields.Proxy_Port = (int) _c.GetValue(ConfigItems.Proxy_Port);
            Fields.Proxy_User = (string) _c.GetValue(ConfigItems.Proxy_User);
            Fields.Proxy_Password = _c.GetEncryptedString(ConfigItems.Proxy_Password);

            string verStr = (string) _c.GetValue(ConfigItems.Elpis_Version);
            System.Version ver;
            if (System.Version.TryParse(verStr, out ver))
                Fields.Elpis_Version = ver;

            Fields.Elpis_InstallID = (string) _c.GetValue(ConfigItems.Elpis_InstallID);
            Fields.Elpis_CheckUpdates = (bool) _c.GetValue(ConfigItems.Elpis_CheckUpdates);
            Fields.Elpis_CheckBetaUpdates = (bool) _c.GetValue(ConfigItems.Elpis_CheckBetaUpdates);
            Fields.Elpis_RemoteControlEnabled = (bool) _c.GetValue(ConfigItems.Elpis_RemoteControlEnabled);
            Fields.Elpis_MinimizeToTray = (bool) _c.GetValue(ConfigItems.Elpis_MinimizeToTray);
            Fields.Elpis_ShowTrayNotifications = (bool) _c.GetValue(ConfigItems.Elpis_ShowTrayNotifications);
            Fields.Elpis_Volume = (int) _c.GetValue(ConfigItems.Elpis_Volume);
            Fields.Elpis_PauseOnLock = (bool) _c.GetValue(ConfigItems.Elpis_PauseOnLock);
            Fields.Elpis_MaxHistory = (int) _c.GetValue(ConfigItems.Elpis_MaxHistory);

            Fields.LastFM_Scrobble = (bool) _c.GetValue(ConfigItems.LastFM_Scrobble);
            Fields.LastFM_SessionKey = _c.GetEncryptedString(ConfigItems.LastFM_SessionKey);

            string location = (string) _c.GetValue(ConfigItems.Elpis_StartupLocation);
            try
            {
                Fields.Elpis_StartupLocation = System.Windows.Point.Parse(location);
            }
            catch
            {
                Fields.Elpis_StartupLocation = new System.Windows.Point(-1, -1);
            }

            string size = (string) _c.GetValue(ConfigItems.Elpis_StartupSize);
            try
            {
                Fields.Elpis_StartupSize = System.Windows.Size.Parse(size);
            }
            catch
            {
                Fields.Elpis_StartupSize = new System.Windows.Size(0, 0);
            }

            System.Collections.Generic.Dictionary<int, string> list =
                _c.GetValue(ConfigItems.HotKeysList) as System.Collections.Generic.Dictionary<int, string>;

            if (list != null)
            {
                foreach (System.Collections.Generic.KeyValuePair<int, string> pair in list)
                {
                    Fields.Elpis_HotKeys.Add(pair.Key, new HotkeyConfig(pair.Value, HotkeyConfig.Default));
                }
            }

            Fields.System_OutputDevice = (string) _c.GetValue(ConfigItems.System_OutputDevice);

            Util.Log.O("Config File Contents:");
            Util.Log.O(_c.LastConfig);

            return true;
        }

        public HotkeyConfig GetKeyObject(Util.MapConfigEntry entry)
        {
            return new HotkeyConfig((string) _c.GetValue(entry), (HotkeyConfig) entry.Default);
        }

        public bool SaveConfig()
        {
            try
            {
                //TODO: These should be commented out later
                _c.SetValue(ConfigItems.RipStream, Fields.RipStream);
                _c.SetValue(ConfigItems.RipPath, Fields.RipPath);

                _c.SetValue(ConfigItems.Debug_WriteLog, Fields.Debug_WriteLog);
                _c.SetValue(ConfigItems.Debug_Logpath, Fields.Debug_Logpath);
                _c.SetValue(ConfigItems.Debug_Timestamp, Fields.Debug_Timestamp);
                //*********************************************

                _c.SetValue(ConfigItems.Login_Email, Fields.Login_Email);
                _c.SetEncryptedString(ConfigItems.Login_Password, Fields.Login_Password);
                _c.SetValue(ConfigItems.Login_AutoLogin, Fields.Login_AutoLogin);

                _c.SetValue(ConfigItems.Pandora_AudioFormat, Fields.Pandora_AudioFormat);
                _c.SetValue(ConfigItems.Pandora_AutoPlay, Fields.Pandora_AutoPlay);
                _c.SetValue(ConfigItems.Pandora_LastStationID, Fields.Pandora_LastStationID);
                _c.SetValue(ConfigItems.Pandora_StationSortOrder, Fields.Pandora_StationSortOrder);

                _c.SetValue(ConfigItems.Proxy_Address, Fields.Proxy_Address.Trim());
                _c.SetValue(ConfigItems.Proxy_Port, Fields.Proxy_Port);
                _c.SetValue(ConfigItems.Proxy_User, Fields.Proxy_User.Trim());
                _c.SetEncryptedString(ConfigItems.Proxy_Password, Fields.Proxy_Password.Trim());

                _c.SetValue(ConfigItems.Elpis_Version, Fields.Elpis_Version.ToString());
                _c.SetValue(ConfigItems.Elpis_CheckUpdates, Fields.Elpis_CheckUpdates);
                _c.SetValue(ConfigItems.Elpis_CheckBetaUpdates, Fields.Elpis_CheckBetaUpdates);
                _c.SetValue(ConfigItems.Elpis_RemoteControlEnabled, Fields.Elpis_RemoteControlEnabled);
                _c.SetValue(ConfigItems.Elpis_MinimizeToTray, Fields.Elpis_MinimizeToTray);
                _c.SetValue(ConfigItems.Elpis_ShowTrayNotifications, Fields.Elpis_ShowTrayNotifications);
                _c.SetValue(ConfigItems.Elpis_PauseOnLock, Fields.Elpis_PauseOnLock);
                _c.SetValue(ConfigItems.Elpis_MaxHistory, Fields.Elpis_MaxHistory);

                _c.SetValue(ConfigItems.LastFM_Scrobble, Fields.LastFM_Scrobble);
                _c.SetEncryptedString(ConfigItems.LastFM_SessionKey, Fields.LastFM_SessionKey);

                _c.SetValue(ConfigItems.Elpis_StartupLocation, Fields.Elpis_StartupLocation.ToString());
                _c.SetValue(ConfigItems.Elpis_StartupSize, Fields.Elpis_StartupSize.ToString());
                _c.SetValue(ConfigItems.Elpis_Volume, Fields.Elpis_Volume);

                System.Collections.Generic.Dictionary<int, string> hotkeysFlattened = Enumerable.ToDictionary(Fields.Elpis_HotKeys, pair => pair.Key, pair => pair.Value.ToString());
                _c.SetValue(ConfigItems.HotKeysList, hotkeysFlattened);

                _c.SetValue(ConfigItems.System_OutputDevice, Fields.System_OutputDevice);
            }
            catch (System.Exception ex)
            {
                Util.Log.O("Error saving config: " + ex);
                return false;
            }

            if (!_c.SaveConfig()) return false;

            Util.Log.O("Config File Contents:");
            Util.Log.O(_c.LastConfig);
            return true;
        }
    }
}