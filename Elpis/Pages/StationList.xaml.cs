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

using Elpis.Wpf.Controls;

namespace Elpis.Wpf.Pages
{
    /// <summary>
    ///     Interaction logic for StationList.xaml
    /// </summary>
    public partial class StationList
    {
        public StationList(PandoraSharpPlayer.Player player)
        {
            _player = player;
            _player.StationLoading += _player_StationLoading;
            _player.ExceptionEvent += _player_ExceptionEvent;
            InitializeComponent();

            _stationMenu = Resources["StationMenu"] as System.Windows.Controls.ContextMenu;
            _mnuRename = _stationMenu?.Items[0] as System.Windows.Controls.MenuItem; //mnuRename
            _mnuDelete = _stationMenu?.Items[1] as System.Windows.Controls.MenuItem; //mnuDelete
            _mnuEditQuickMix = _stationMenu?.Items[2] as System.Windows.Controls.MenuItem; //mnuEditQuickMix
            _mnuAddVariety = _stationMenu?.Items[3] as System.Windows.Controls.MenuItem; //mnuAddVariety
            _mnuInfo = _stationMenu?.Items[4] as System.Windows.Controls.MenuItem; //mnuInfo
        }

        public delegate void AddVarietyEventHandler(PandoraSharp.Station station);

        public delegate void EditQuickMixEventHandler();

        public System.Collections.Generic.List<PandoraSharp.Station> Stations
        {
            get { return (System.Collections.Generic.List<PandoraSharp.Station>) StationItems.ItemsSource; }
            set
            {
                this.BeginDispatch(() =>
                {
                    if (value == null) return;

                    lblNoStations.Visibility = value.Count > 0
                        ? System.Windows.Visibility.Hidden
                        : System.Windows.Visibility.Visible;
                    StationItems.ItemsSource = value;
                    _currSort = _player.StationSortOrder;
                    scrollMain.ScrollToHome();
                    if (!_waiting) return;
                    ShowWait(false);
                    _waiting = false;
                });
            }
        }

        private readonly PandoraSharpPlayer.Player _player;
        private PandoraSharp.Station _currMenuStation;

        private PandoraSharp.Pandora.SortOrder _currSort = PandoraSharp.Pandora.SortOrder.DateDesc;
        private System.Windows.Controls.Control _currStationItem;
        private readonly System.Windows.Controls.MenuItem _mnuAddVariety;
        private readonly System.Windows.Controls.MenuItem _mnuDelete;
        private readonly System.Windows.Controls.MenuItem _mnuEditQuickMix;
        private System.Windows.Controls.MenuItem _mnuInfo;
        private readonly System.Windows.Controls.MenuItem _mnuRename;

        private readonly System.Windows.Controls.ContextMenu _stationMenu;
        private bool _waiting;
        public event EditQuickMixEventHandler EditQuickMixEvent;
        public event AddVarietyEventHandler AddVarietyEvent;

        public void SetStationsRefreshing()
        {
            this.BeginDispatch(() =>
            {
                _waiting = true;
                ShowWait(true);
            });
        }

        public void ShowWait(bool state)
        {
            this.BeginDispatch(
                () =>
                {
                    WaitScreen.Visibility = state
                        ? System.Windows.Visibility.Visible
                        : System.Windows.Visibility.Collapsed;
                });
        }

        private void _player_StationLoading(object sender, PandoraSharp.Station station)
        {
            this.BeginDispatch(() =>
            {
                if (IsLoaded)
                    ShowWait(true);
            });
        }

        private void _player_ExceptionEvent(object sender, Util.ErrorCodes code, System.Exception ex)
        {
            ShowWait(false);
        }

        private void StationList_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CloseRename();
            ShowWait(false);
            if (_player.StationSortOrder != _currSort)
                _player.RefreshStations();
        }

        private void StationList_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CloseRename();
            ShowWait(false);
        }

        private void StationItem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PandoraSharp.Station station = (PandoraSharp.Station) ((System.Windows.Controls.Grid) sender).DataContext;

            _player.PlayStation(station);
        }

        private void CloseRename()
        {
            if (_currMenuStation == null || _currStationItem == null) return;

            System.Windows.Controls.Button btnSaveRename =
                _currStationItem.FindChildByName<System.Windows.Controls.Button>("btnSaveRename");
            System.Windows.Controls.TextBlock txtStationName =
                _currStationItem.FindChildByName<System.Windows.Controls.TextBlock>("txtStationName");
            System.Windows.Controls.TextBox txtRename =
                _currStationItem.FindChildByName<System.Windows.Controls.TextBox>("txtRename");

            txtStationName.Visibility = System.Windows.Visibility.Visible;
            txtRename.Visibility = System.Windows.Visibility.Hidden;
            btnSaveRename.Visibility = System.Windows.Visibility.Hidden;
        }

        private void DoRename()
        {
            if (_currMenuStation == null || _currStationItem == null) return;

            string name = _currStationItem.FindChildByName<System.Windows.Controls.TextBox>("txtRename").Text;

            _player.StationRename(_currMenuStation, name);

            System.Windows.Controls.TextBlock txtStationName =
                _currStationItem.FindChildByName<System.Windows.Controls.TextBlock>("txtStationName");

            txtStationName.Text = name;

            CloseRename();
        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                DoRename();
        }

        private void btnSaveRename_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            e.Handled = true;

            DoRename();
        }

        private void ShowMenu(object sender)
        {
            if (_currMenuStation != null)
            {
                _mnuRename.Visibility = _currMenuStation.IsQuickMix
                    ? System.Windows.Visibility.Collapsed
                    : System.Windows.Visibility.Visible;
                _mnuDelete.Visibility = _currMenuStation.IsQuickMix
                    ? System.Windows.Visibility.Collapsed
                    : System.Windows.Visibility.Visible;
                _mnuAddVariety.Visibility = _currMenuStation.IsQuickMix
                    ? System.Windows.Visibility.Collapsed
                    : System.Windows.Visibility.Visible;
                _mnuEditQuickMix.Visibility = _currMenuStation.IsQuickMix
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
            }

            _stationMenu.PlacementTarget = sender as System.Windows.UIElement;
            _stationMenu.IsOpen = true;
        }

        private System.Windows.Controls.Control GetStationItem(object sender)
        {
            return
                ((ImageButton) sender).FindParentByName<System.Windows.Controls.ContentControl>("StationItem");
        }

        private PandoraSharp.Station GetItemStation(object sender)
        {
            return GetStationItem(sender).DataContext as PandoraSharp.Station;
        }

        private void btnMenu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CloseRename();

            _currMenuStation = GetItemStation(sender);
            _currStationItem = GetStationItem(sender);

            ShowMenu(sender);
        }

        private void StationMenu_Closed(object sender, System.Windows.RoutedEventArgs e)
        {
            //_currMenuStation = null;
            //_currStationItem = null;
        }

        private void mnuRename_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currStationItem == null) return;

            System.Windows.Controls.TextBlock textStation =
                _currStationItem.FindChildByName<System.Windows.Controls.TextBlock>("txtStationName");
            System.Windows.Controls.TextBox textBox =
                _currStationItem.FindChildByName<System.Windows.Controls.TextBox>("txtRename");
            System.Windows.Controls.Button saverename =
                _currStationItem.FindChildByName<System.Windows.Controls.Button>("btnSaveRename");

            textBox.Text = textStation.Text;
            textStation.Visibility = System.Windows.Visibility.Hidden;
            textBox.Visibility = System.Windows.Visibility.Visible;
            saverename.Visibility = System.Windows.Visibility.Visible;
            textBox.Focus();
            textBox.SelectAll();
        }

        private void mnuDelete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuStation != null)
            {
                _waiting = true;
                ShowWait(true);
                _player.StationDelete(_currMenuStation);
            }
        }

        private void mnuEditQuickMix_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EditQuickMixEvent?.Invoke();
        }

        private void mnuInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuStation != null && _currMenuStation.InfoUrl.StartsWith("http"))
                System.Diagnostics.Process.Start(_currMenuStation.InfoUrl);
        }

        private void mnuAddVariety_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuStation != null)
            {
                AddVarietyEvent?.Invoke(_currMenuStation);
            }
        }

        private void mnuMakeShortcut_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            /*TODO: Add some kind of visual feedback to make it obvious creating the shortcut succeeded*/
            if (_currMenuStation == null)
            {
                _currMenuStation?.CreateShortcut();
            }
        }
    }
}