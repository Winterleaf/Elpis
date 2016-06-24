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

using System.Linq;

namespace Elpis.Wpf.Pages
{
    /// <summary>
    ///     Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings
    {
        public Settings(PandoraSharpPlayer.Player player, Config config, HotKeyHost keyHost)
        {
            InitializeComponent();

            _config = config;
            _player = player;
            _keyHost = keyHost;
            HotKeyItems.SetBinding(System.Windows.Controls.ItemsControl.ItemsSourceProperty,
                new System.Windows.Data.Binding("HotKeys")
                {
                    Source = _keyHost,
                    NotifyOnSourceUpdated = true,
                    Mode = System.Windows.Data.BindingMode.OneWay
                });
        }

        private readonly Config _config;
        private readonly HotKeyHost _keyHost;

        private readonly PandoraSharpPlayer.Player _player;

        public event CloseEvent Close;

        public event RestartEvent Restart;

        public event LogoutEvent Logout;

        public event LastFmAuthRequestEvent LastFmAuthRequest;

        public event LasFmDeAuthRequestEvent LasFmDeAuthRequest;

        private void LoadConfig()
        {
            chkRipStream.IsChecked = _config.Fields.RipStream;
            txtRipPath.Text = _config.Fields.RipPath;

            chkAutoLogin.IsChecked = _config.Fields.Login_AutoLogin;
            cmbAudioFormat.SelectedValue = _config.Fields.Pandora_AudioFormat;
            cmbStationSort.SelectedValue = _config.Fields.Pandora_StationSortOrder;

            chkAutoPlay.IsChecked = _config.Fields.Pandora_AutoPlay;
            chkCheckUpdates.IsChecked = _config.Fields.Elpis_CheckUpdates;
            chkTrayMinimize.IsChecked = _config.Fields.Elpis_MinimizeToTray;
            chkShowNotify.IsChecked = _config.Fields.Elpis_ShowTrayNotifications;
            chkPauseOnLock.IsChecked = _config.Fields.Elpis_PauseOnLock;
            chkCheckBetaUpdates.IsChecked = _config.Fields.Elpis_CheckBetaUpdates;
            chkRemoteControlEnabled.IsChecked = _config.Fields.Elpis_RemoteControlEnabled;

            _config.Fields.Pandora_AudioFormat = _player.AudioFormat;

            _config.Fields.Pandora_StationSortOrder = _config.Fields.Pandora_StationSortOrder;

            txtProxyAddress.Text = _config.Fields.Proxy_Address;
            txtProxyPort.Text = _config.Fields.Proxy_Port.ToString();
            txtProxyUser.Text = _config.Fields.Proxy_User;
            txtProxyPassword.Password = _config.Fields.Proxy_Password;

            chkEnableScrobbler.IsChecked = _config.Fields.LastFM_Scrobble;

            txtIPAddress.ItemsSource = getLocalIPAddresses();

            // Build list of all output devices
            cmbOutputDevice.Items.Clear();
            foreach (string device in _player.GetOutputDevices())
                cmbOutputDevice.Items.Add(device);

            // Get current output device
            cmbOutputDevice.SelectedValue = _player.OutputDevice;

            _config.SaveConfig();

            UpdateLastFmControlState();
        }

        private System.Collections.Generic.List<string> getLocalIPAddresses()
        {
            System.Collections.Generic.List<string> ips = new System.Collections.Generic.List<string>();
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return ips;
            try
            {
                System.Net.IPHostEntry host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                ips.AddRange(from ip in host.AddressList where !(ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal || ip.IsIPv6Teredo) select ip.ToString());
            }
            catch (System.Net.Sockets.SocketException e)
            {
                System.Console.WriteLine(@"There was a socket error attempting to get local ips: " + e);
            }
            return ips;
        }

        private void SaveConfig()
        {
            System.Diagnostics.Debug.Assert(chkRipStream.IsChecked != null, "chkRipStream.IsChecked != null");
            _config.Fields.RipStream = (bool) chkRipStream.IsChecked;
            _config.Fields.RipPath = txtRipPath.Text;

            System.Diagnostics.Debug.Assert(chkAutoLogin.IsChecked != null, "chkAutoLogin.IsChecked != null");
            _config.Fields.Login_AutoLogin = (bool) chkAutoLogin.IsChecked;
            _config.Fields.Pandora_AudioFormat = (string) cmbAudioFormat.SelectedValue;
            _config.Fields.Pandora_StationSortOrder = (string) cmbStationSort.SelectedValue;
            System.Diagnostics.Debug.Assert(chkAutoPlay.IsChecked != null, "chkAutoPlay.IsChecked != null");
            if (!_config.Fields.Pandora_AutoPlay && (bool) chkAutoPlay.IsChecked && _player.CurrentStation != null)
                _config.Fields.Pandora_LastStationID = _player.CurrentStation.Id;
            _config.Fields.Pandora_AutoPlay = (bool) chkAutoPlay.IsChecked;
            System.Diagnostics.Debug.Assert(chkCheckUpdates.IsChecked != null, "chkCheckUpdates.IsChecked != null");
            _config.Fields.Elpis_CheckUpdates = (bool) chkCheckUpdates.IsChecked;
            System.Diagnostics.Debug.Assert(chkCheckBetaUpdates.IsChecked != null, "chkCheckBetaUpdates.IsChecked != null");
            _config.Fields.Elpis_CheckBetaUpdates = (bool) chkCheckBetaUpdates.IsChecked;
            System.Diagnostics.Debug.Assert(chkRemoteControlEnabled.IsChecked != null, "chkRemoteControlEnabled.IsChecked != null");
            _config.Fields.Elpis_RemoteControlEnabled = (bool) chkRemoteControlEnabled.IsChecked;
            System.Diagnostics.Debug.Assert(chkTrayMinimize.IsChecked != null, "chkTrayMinimize.IsChecked != null");
            _config.Fields.Elpis_MinimizeToTray = (bool) chkTrayMinimize.IsChecked;
            System.Diagnostics.Debug.Assert(chkShowNotify.IsChecked != null, "chkShowNotify.IsChecked != null");
            _config.Fields.Elpis_ShowTrayNotifications = (bool) chkShowNotify.IsChecked;
            System.Diagnostics.Debug.Assert(chkPauseOnLock.IsChecked != null, "chkPauseOnLock.IsChecked != null");
            _player.PauseOnLock = _config.Fields.Elpis_PauseOnLock = (bool) chkPauseOnLock.IsChecked;

            _player.AudioFormat = _config.Fields.Pandora_AudioFormat;
            //In case MP3-HiFi was rejected
            _config.Fields.Pandora_AudioFormat = _player.AudioFormat;

            _player.SetStationSortOrder(_config.Fields.Pandora_StationSortOrder);
            _config.Fields.Pandora_StationSortOrder = _player.StationSortOrder.ToString();

            _config.Fields.Proxy_Address = txtProxyAddress.Text;
            int port = _config.Fields.Proxy_Port;
            int.TryParse(txtProxyPort.Text, out port);
            _config.Fields.Proxy_Port = port;
            _config.Fields.Proxy_User = txtProxyUser.Text;
            _config.Fields.Proxy_Password = txtProxyPassword.Password;

            System.Diagnostics.Debug.Assert(chkEnableScrobbler.IsChecked != null, "chkEnableScrobbler.IsChecked != null");
            _config.Fields.LastFM_Scrobble = (bool) chkEnableScrobbler.IsChecked;
            System.Collections.Generic.Dictionary<int, HotkeyConfig> keys =
                new System.Collections.Generic.Dictionary<int, HotkeyConfig>();
            foreach (System.Collections.Generic.KeyValuePair<int, HotKey> pair in _keyHost.HotKeys)
            {
                keys.Add(pair.Key, new HotkeyConfig(pair.Value));
            }
            _config.Fields.Elpis_HotKeys = keys;

            if (!_config.Fields.System_OutputDevice.Equals((string) cmbOutputDevice.SelectedValue))
            {
                _config.Fields.System_OutputDevice = (string) cmbOutputDevice.SelectedValue;
                _player.OutputDevice = (string) cmbOutputDevice.SelectedValue;
            }

            _config.SaveConfig();
        }

        private bool NeedsRestart()
        {
            bool restart = txtProxyAddress.Text != _config.Fields.Proxy_Address ||
                           txtProxyPort.Text != _config.Fields.Proxy_Port.ToString() ||
                           txtProxyUser.Text != _config.Fields.Proxy_User ||
                           txtProxyPassword.Password != _config.Fields.Proxy_Password ||
                           chkRemoteControlEnabled.IsChecked != _config.Fields.Elpis_RemoteControlEnabled;

            return restart;
        }

        private void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!System.IO.Directory.Exists(txtRipPath.Text) && chkRipStream.IsChecked == true)
            {
                chkRipStream.IsChecked = false;
                txtRipPath.Text = "Invalid";
                return;
            }
            if (cmbAudioFormat.SelectedIndex == 0 && chkRipStream.IsChecked == true)
            {
                chkRipStream.IsChecked = false;
                return;
            }

            bool restart = NeedsRestart();
            SaveConfig();

            if (restart)
            {
                Restart?.Invoke();
            }
            else
            {
                Close?.Invoke();
            }
        }

        private void btnLogout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _config.Fields.Login_Email = string.Empty;
            _config.Fields.Login_Password = string.Empty;

            SaveConfig();

            Logout?.Invoke();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            btnLogout.IsEnabled = _player.LoggedIn;
            LoadConfig();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e) {}

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close?.Invoke();
        }

        private void txtProxyPort_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                System.Convert.ToInt32(e.Text);
                string text = txtProxyPort.Text + e.Text;
                int output = System.Convert.ToInt32(text);
                if (output < 0 || output > 65535)
                    e.Handled = true;
            }
            catch
            {
                e.Handled = true;
            }
        }

        private void ShowLastFmAuthButton(bool state)
        {
            btnLastFMAuth.Visibility = state ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            btnLastFMDisable.Visibility = state ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
        }

        private void UpdateLastFmControlState()
        {
            System.Diagnostics.Debug.Assert(chkEnableScrobbler.IsChecked != null, "chkEnableScrobbler.IsChecked != null");
            bool state = (bool) chkEnableScrobbler.IsChecked;

            ShowLastFmAuthButton(_config.Fields.LastFM_SessionKey == string.Empty);
            btnLastFMAuth.IsEnabled = state || _config.Fields.LastFM_SessionKey != string.Empty;
        }

        private void chkEnableScrobbler_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateLastFmControlState();
        }

        private void btnLastFMAuth_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LastFmAuthRequest?.Invoke();
        }

        private void btnLastFMDisable_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LasFmDeAuthRequest?.Invoke();

            chkEnableScrobbler.IsChecked = false;
            UpdateLastFmControlState();
        }

        private void btnAddHotKey_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _keyHost.AddHotKey(new HotKey(PlayerCommands.PlayPause, System.Windows.Input.Key.None,
                System.Windows.Input.ModifierKeys.None));
        }

        //private void btnDelHotkey_Click(object sender, RoutedEventArgs e)
        //{
        //    KeyValuePair<int, HotKey> pair = (KeyValuePair<int, HotKey>) ((FrameworkElement)sender).DataContext;
        //    _keyHost.RemoveHotKey(pair.Value);
        //}

        private void RemoveHotkey_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Collections.Generic.KeyValuePair<int, HotKey> pair =
                (System.Collections.Generic.KeyValuePair<int, HotKey>)
                    ((System.Windows.FrameworkElement) sender).DataContext;
            _keyHost.RemoveHotKey(pair.Value);
        }

        private void txtIPAddress_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.C &&
                System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                System.Windows.Clipboard.SetText(txtIPAddress.SelectedItem.ToString());
            }
        }

        #region Delegates

        public delegate void CloseEvent();

        public delegate void RestartEvent();

        public delegate void LogoutEvent();

        public delegate void LastFmAuthRequestEvent();

        public delegate void LasFmDeAuthRequestEvent();

        #endregion
    }

    public class HotKeyBox : System.Windows.Controls.TextBox
    {
        static HotKeyBox()
        {
            TextProperty.OverrideMetadata(typeof (HotKeyBox),
                new System.Windows.FrameworkPropertyMetadata
                {
                    BindsTwoWayByDefault = false,
                    Journal = true,
                    DefaultUpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                });
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            System.Collections.Generic.KeyValuePair<int, HotKey> pair =
                (System.Collections.Generic.KeyValuePair<int, HotKey>) DataContext;
            HotKey h = pair.Value;
            switch (e.Key)
            {
                case System.Windows.Input.Key.LeftShift:
                case System.Windows.Input.Key.LeftAlt:
                case System.Windows.Input.Key.LeftCtrl:
                case System.Windows.Input.Key.RightCtrl:
                case System.Windows.Input.Key.RightAlt:
                case System.Windows.Input.Key.RightShift:
                    break;
                default:
                    try
                    {
                        h.SetKeyCombo(e.Key, System.Windows.Input.Keyboard.Modifiers);
                        e.Handled = true;
                        GetBindingExpression(TextProperty)?.UpdateTarget();
                        //HACK: This is a cheap-and-nasty way to shift focus from the textbox
                        IsEnabled = false;
                        IsEnabled = true;
                    }
                    catch (HotKeyNotSupportedException)
                    {
                        //todo
                        //Log.O(es.Message);
                    }
                    break;
            }
        }
    }
}