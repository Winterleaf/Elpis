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

namespace Elpis
{
    /// <summary>
    ///     Interaction logic for MainBar.xaml
    /// </summary>
    public partial class MainBar
    {
        public MainBar()
        {
            InitializeComponent();

            _errorMenu = Resources["ErrorMenu"] as System.Windows.Controls.ContextMenu;
            System.Diagnostics.Debug.Assert(_errorMenu != null, "_errorMenu != null");
            _errorMenu.PlacementTarget = btnError;
            _volCloseTimer = new System.Timers.Timer(2000);
            _volCloseTimer.Elapsed += volCloseTimer_Elapsed;
        }

        public double Volume
        {
            get { return sVolume.Value; }
            set { sVolume.Value = value; }
        }

        private readonly System.Windows.Controls.ContextMenu _errorMenu;
        private readonly System.Timers.Timer _volCloseTimer;

        public event MainBarHandler StationListClick;
        public event MainBarHandler CreateStationClick;
        public event MainBarHandler PlayPauseClick;
        public event MainBarHandler NextClick;
        public event MainBarHandler SettingsClick;
        public event MainBarHandler AboutClick;

        public event VolumeChangedHandler VolumeChanged;

        public event ErrorClickedHandler ErrorClicked;

        public void ShowError(string msg)
        {
            this.BeginDispatch(() =>
            {
                btnError.ToolTip = msg;
                gridError.Visibility = System.Windows.Visibility.Visible;
            });
        }

        private void btnStationList_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StationListClick?.Invoke();
        }

        private void btnStationListClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StationListClick?.Invoke();
        }

        private void btnPlayPause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PlayPauseClick?.Invoke();
        }

        private void btnNext_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NextClick?.Invoke();
        }

        private void btnSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SettingsClick?.Invoke();
        }

        private void btnAbout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AboutClick?.Invoke();
        }

        private void btnCreateStation_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CreateStationClick?.Invoke();
        }

        private void btnCreateStationClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CreateStationClick?.Invoke();
        }

        private void btnError_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _errorMenu.IsOpen = true;
        }

        private void ShowError_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ErrorClicked?.Invoke();

            gridError.Visibility = System.Windows.Visibility.Hidden;
        }

        private void DismissError_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            gridError.Visibility = System.Windows.Visibility.Hidden;
        }

        private void sVolume_VolumeChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeChanged?.Invoke(e.NewValue);

            if (System.Math.Abs(Volume) < .0001d)
                imgVolume.Source =
                    new System.Windows.Media.Imaging.BitmapImage(Resources["Image_Volume_0"] as System.Uri);
            else if (Volume > 0 && Volume < 33)
                imgVolume.Source =
                    new System.Windows.Media.Imaging.BitmapImage(Resources["Image_Volume_33"] as System.Uri);
            else if (Volume >= 33 && Volume < 66)
                imgVolume.Source =
                    new System.Windows.Media.Imaging.BitmapImage(Resources["Image_Volume_66"] as System.Uri);
            else if (Volume >= 66)
                imgVolume.Source =
                    new System.Windows.Media.Imaging.BitmapImage(Resources["Image_Volume_100"] as System.Uri);
        }

        private void btnVolume_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _volCloseTimer.Stop();
            gridVolume.Visibility = System.Windows.Visibility.Visible;
        }

        private void volCloseTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.BeginDispatch(() => { gridVolume.Visibility = System.Windows.Visibility.Hidden; });
        }

        private void gridVolume_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _volCloseTimer.Stop();
            _volCloseTimer.Start();
        }

        private void gridVolume_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _volCloseTimer.Stop();
        }

        #region Delegates

        public delegate void MainBarHandler();

        public delegate void VolumeChangedHandler(double vol);

        public delegate void ErrorClickedHandler();

        #endregion

        #region ItemStates

        private static System.Windows.Visibility Vis(bool state)
        {
            return state ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        }

        private void ShowAbout(bool state)
        {
            btnAbout.Visibility = Vis(state);
        }

        private void ShowSettings(bool state)
        {
            btnSettings.Visibility = Vis(state);
        }

        private void ShowNext(bool state)
        {
            btnNext.Visibility = Vis(state);
        }

        private void ShowPlayControls(bool state)
        {
            gridPlayPause.Visibility = Vis(state);

            btnVolume.Visibility = Vis(state);
        }

        private void ShowStationList(bool state)
        {
            btnStationList.Visibility = Vis(state);
        }

        private void ShowStationListClose(bool state)
        {
            btnStationListClose.Visibility = Vis(state);
        }

        private void ShowCreateStation(bool state)
        {
            btnCreateStation.Visibility = Vis(state);
        }

        private void ShowCreateStationClose(bool state)
        {
            //btnCreateStationClose.Visibility = Vis(state);
        }

        public void SetPlaying(bool state)
        {
            ShowPlayControls(state);
        }

        public void SetModeLoading()
        {
            ShowAbout(false);
            ShowSettings(false);

            ShowStationList(false);
            ShowStationListClose(false);
            ShowCreateStation(false);
            ShowCreateStationClose(false);
        }

        public void SetModePlayList()
        {
            ShowAbout(true);
            ShowSettings(true);

            //This will not go away once set, that's intended
            SetPlaying(true);
            ShowStationList(true);
            ShowStationListClose(false);
            ShowCreateStation(true);
            ShowCreateStationClose(false);
        }

        public void SetModeStationList(bool stationLoaded)
        {
            ShowAbout(true);
            ShowSettings(true);

            ShowStationList(!stationLoaded);
            ShowStationListClose(stationLoaded);
            ShowCreateStation(true);
            ShowCreateStationClose(false);
        }

        public void SetModeSearch()
        {
            ShowAbout(true);
            ShowSettings(true);

            ShowStationList(true);
            ShowStationListClose(false);
            ShowCreateStation(false);
            ShowCreateStationClose(true);
        }

        public void SetModeLogin()
        {
            ShowAbout(true);
            ShowSettings(true);

            ShowStationList(false);
            ShowStationListClose(false);
            ShowCreateStation(false);
            ShowCreateStationClose(false);
        }

        public void SetModeSettings()
        {
            ShowAbout(true);
            ShowSettings(true);

            ShowStationList(false);
            ShowStationListClose(false);
            ShowCreateStation(false);
            ShowCreateStationClose(false);
        }

        public void SetModeAbout()
        {
            ShowAbout(true);
            ShowSettings(true);

            ShowStationList(false);
            ShowStationListClose(false);
            ShowCreateStation(false);
            ShowCreateStationClose(false);
        }

        #endregion
    }
}