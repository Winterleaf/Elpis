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

namespace Elpis.Pages
{
    /// <summary>
    ///     Interaction logic for PlaylistPage.xaml
    /// </summary>
    public partial class PlaylistPage
    {
        public PlaylistPage(PandoraSharpPlayer.Player player)
        {
            _player = player;
            _player.SongStarted += _player_SongStarted;
            _player.PlaybackStateChanged += _player_PlaybackStateChanged;
            _player.PlayedSongAdded += _player_PlayedSongAdded;
            _player.PlayedSongRemoved += _player_PlayedSongRemoved;
            _player.LoadingNextSong += _player_LoadingNextSong;
            _player.FeedbackUpdateEvent += _player_FeedbackUpdateEvent;
            _player.PlaybackProgress += _player_PlaybackProgress;
            _player.StationLoaded += _player_StationLoaded;
            _player.StationLoading += _player_StationLoading;
            _player.ExceptionEvent += _player_ExceptionEvent;
            _player.LogoutEvent += _player_LogoutEvent;
            InitializeComponent();

            _feedbackMap = new System.Collections.Generic.Dictionary<PandoraSharp.Song, Controls.ImageButton[]>();

            _songMenu = Resources["SongMenu"] as System.Windows.Controls.ContextMenu;
            //This would need to be changed if the menu order is ever changed
            System.Diagnostics.Debug.Assert(_songMenu != null, "_songMenu != null");
            _purchaseMenu = _songMenu.Items[0] as System.Windows.Controls.MenuItem;
            System.Diagnostics.Debug.Assert(_purchaseMenu != null, "_purchaseMenu != null");
            _purchaseAmazonAlbum = _purchaseMenu.Items[0] as System.Windows.Controls.MenuItem;
            _purchaseAmazonTrack = _purchaseMenu.Items[1] as System.Windows.Controls.MenuItem;
        }

        private readonly object _currSongLock = new object();
        private readonly System.Collections.Generic.Dictionary<PandoraSharp.Song, Controls.ImageButton[]> _feedbackMap;
        private readonly PandoraSharpPlayer.Player _player;
        private PandoraSharp.Song _currMenuSong;
        private PandoraSharp.Song _currSong;
        private readonly System.Windows.Controls.MenuItem _purchaseAmazonAlbum;
        private readonly System.Windows.Controls.MenuItem _purchaseAmazonTrack;
        private readonly System.Windows.Controls.MenuItem _purchaseMenu;

        private readonly System.Windows.Controls.ContextMenu _songMenu;

        private void ShowWait(bool state)
        {
            this.BeginDispatch(
                () =>
                    StationWaitScreen.Visibility =
                        state ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden);
        }

        private void _player_LogoutEvent(object sender)
        {
            lstOldSongs.Items.Clear();
        }

        private void _player_ExceptionEvent(object sender, Util.ErrorCodes code, System.Exception ex)
        {
            ShowWait(false);
        }

        private void _player_PlaybackStateChanged(object sender, BassPlayer.BassAudioEngine.PlayState oldState,
            BassPlayer.BassAudioEngine.PlayState newState)
        {
            lock (_currSongLock)
            {
                if (newState == BassPlayer.BassAudioEngine.PlayState.Playing && _currSong != null)
                    this.BeginDispatch(() => SetSong(_currSong));
            }
        }

        private void _player_StationLoaded(object sender, PandoraSharp.Station station)
        {
            this.BeginDispatch(() =>
            {
                txtStationName.Text = station.Name;
                ShowWait(false);
            });
        }

        private void _player_StationLoading(object sender, PandoraSharp.Station station)
        {
            this.BeginDispatch(() =>
            {
                if (IsLoaded)
                {
                    ShowWait(true);
                }
            });
        }

        private void _player_PlaybackProgress(object sender, BassPlayer.BassAudioEngine.Progress prog)
        {
            UpdateProgress(prog);
        }

        private void UpdateProgress(BassPlayer.BassAudioEngine.Progress prog)
        {
            this.BeginDispatch(() =>
            {
                lblCurrTime.Content = prog.ElapsedTime.ToString(@"mm\:ss");
                lblRemainTime.Content = prog.RemainingTime.ToString(@"mm\:ss");
                progPlayTime.Value = prog.Percent;
            });
        }

        private void _player_FeedbackUpdateEvent(object sender, PandoraSharp.Song song, bool success)
        {
            this.BeginDispatch(() =>
            {
                if (!_feedbackMap.ContainsKey(song)) return;
                //bit of a hack, but avoids putting INotify in lower level classes or making wrappers
                foreach (Controls.ImageButton button in _feedbackMap[song])
                {
                    Controls.ContentSpinner spinner = button.FindParent<Controls.ContentSpinner>();
                    System.Windows.Data.BindingExpression bind =
                        button.GetBindingExpression(Controls.ImageButton.IsActiveProperty);
                    bind?.UpdateTarget();
                    spinner.StopAnimation();
                }
                _feedbackMap.Remove(song);

                if (song.Banned && song == _player.CurrentSong) _player.Next();
            });
        }

        private void _player_LoadingNextSong(object sender)
        {
            UpdateProgress(new BassPlayer.BassAudioEngine.Progress
            {
                ElapsedTime = new System.TimeSpan(0),
                TotalTime = new System.TimeSpan(0)
            });
            this.BeginDispatch(() =>
            {
                CurrentSong.Visibility = System.Windows.Visibility.Hidden;
                WaitScreen.Visibility = System.Windows.Visibility.Visible;
            });
        }

        private void _player_PlayedSongRemoved(object sender, PandoraSharp.Song song)
        {
            this.BeginDispatch(() => PlayedSongRemove(song));
        }

        private void PlayedSongRemove(PandoraSharp.Song song)
        {
            System.Windows.Controls.ContentControl result = Enumerable.FirstOrDefault(Enumerable.Cast<System.Windows.Controls.ContentControl>(lstOldSongs.Items), sc => song == sc.Content);

            if (result == null) return;
            bool last = lstOldSongs.Items.IndexOf(result) == lstOldSongs.Items.Count - 1;
            Dispatcher.Invoke(AnimateListRemove(result, last));
        }

        private System.Action AnimateListRemove(System.Windows.Controls.ContentControl item, bool last)
        {
            return () =>
            {
                System.Windows.Media.Animation.Storyboard remSB;
                if (last)
                {
                    remSB = ((System.Windows.Media.Animation.Storyboard) Resources["ListBoxRemoveLast"]).Clone();
                }
                else
                {
                    remSB = ((System.Windows.Media.Animation.Storyboard) Resources["ListBoxRemove"]).Clone();
                    ((System.Windows.Media.Animation.DoubleAnimation) remSB.Children[1]).From = item.ActualHeight;
                }

                remSB.Completed += (o, e) => lstOldSongs.Items.Remove(item);
                remSB.Begin(item);
            };
        }

        private void _player_PlayedSongAdded(object sender, PandoraSharp.Song song)
        {
            this.BeginDispatch(() => PlayedSongAdd(song));
        }

        private void PlayedSongAdd(PandoraSharp.Song song)
        {
            System.Windows.Controls.ContentControl songControl = new System.Windows.Controls.ContentControl();
            System.Windows.RoutedEventHandler loadEvent = AnimateListAdd(lstOldSongs.Items.Count == 0);
            songControl.Loaded += loadEvent;
            songControl.Tag = loadEvent;
            songControl.ContentTemplate = (System.Windows.DataTemplate) Resources["SongTemplate"];
            songControl.Content = song;

            lstOldSongs.Items.Insert(0, songControl);
        }

        private System.Windows.RoutedEventHandler AnimateListAdd(bool first)
        {
            return (o1, e1) =>
            {
                System.Windows.Media.Animation.Storyboard addSB = first ? ((System.Windows.Media.Animation.Storyboard) Resources["ListBoxAddFirst"]).Clone() : ((System.Windows.Media.Animation.Storyboard) Resources["ListBoxAdd"]).Clone();
                addSB.Begin((System.Windows.Controls.ContentControl) o1);

                System.Windows.Controls.ContentControl song = (System.Windows.Controls.ContentControl) o1;
                song.Loaded -= (System.Windows.RoutedEventHandler) song.Tag;
            };
        }

        private void SetSong(PandoraSharp.Song song)
        {
            CurrentSong.Content = song;
            CurrentSong.Visibility = System.Windows.Visibility.Visible;
            WaitScreen.Visibility = System.Windows.Visibility.Collapsed;

            this.BeginDispatch(() =>
            {
                string[] stat = txtStationName.Text.Split('-');
                if (stat[0].Equals("Quick Mix"))
                {
                    txtStationName.Text = stat[0] + "-" + song.Station.Name;
                }
            });
        }

        private void _player_SongStarted(object sender, PandoraSharp.Song song)
        {
            lock (_currSongLock)
            {
                _currSong = song;
            }
        }

        private void RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        public void ThumbDownCurrent()
        {
            Controls.ImageButton thumbDown = CurrentSong.FindChildByName<Controls.ImageButton>("btnThumbDown");
            ThumbDownHandle(thumbDown);
        }

        private void ThumbDownHandle(Controls.ImageButton button)
        {
            PandoraSharp.Song song =
                (PandoraSharp.Song) button.FindParentByName<System.Windows.Controls.Grid>("SongItem").DataContext;
            Controls.ContentSpinner spinner = button.FindParent<Controls.ContentSpinner>();
            if (_feedbackMap.ContainsKey(song)) return;

            Controls.ImageButton otherButton =
                spinner.FindSiblingByName<Controls.ContentSpinner>("SpinUp")
                    .FindChildByName<Controls.ImageButton>("btnThumbUp");
            _feedbackMap.Add(song, new[] {button, otherButton});

            if (song.Banned)
                _player.SongDeleteFeedback(song);
            else
                _player.SongThumbDown(song);

            spinner.StartAnimation();
        }

        private void btnThumbDown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ThumbDownHandle((Controls.ImageButton) sender);
        }

        public void ThumbUpCurrent()
        {
            Controls.ImageButton thumbUp = CurrentSong.FindChildByName<Controls.ImageButton>("btnThumbUp");
            ThumbUpHandle(thumbUp);
        }

        private void ThumbUpHandle(Controls.ImageButton button)
        {
            PandoraSharp.Song song =
                (PandoraSharp.Song) button.FindParentByName<System.Windows.Controls.Grid>("SongItem").DataContext;
            Controls.ContentSpinner spinner = button.FindParent<Controls.ContentSpinner>();
            if (_feedbackMap.ContainsKey(song)) return;

            Controls.ImageButton otherButton =
                spinner.FindSiblingByName<Controls.ContentSpinner>("SpinDown")
                    .FindChildByName<Controls.ImageButton>("btnThumbDown");
            _feedbackMap.Add(song, new[] {button, otherButton});
            if (song.Loved)
                _player.SongDeleteFeedback(song);
            else
                _player.SongThumbUp(song);

            spinner.StartAnimation();
        }

        private void btnThumbUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ThumbUpHandle((Controls.ImageButton) sender);
        }

        private PandoraSharp.Song GetItemSong(object sender)
        {
            return
                (PandoraSharp.Song)
                    ((Controls.ImageButton) sender).FindParentByName<System.Windows.Controls.Grid>("SongItem")
                        .DataContext;
        }

        private void ShowMenu(object sender)
        {
            _songMenu.PlacementTarget = sender as System.Windows.UIElement;
            bool showAmazonAlbum = _currMenuSong.AmazonAlbumId != string.Empty;
            bool showAmazonTrack = _currMenuSong.AmazonTrackId != string.Empty;
            bool showPurchase = showAmazonAlbum || showAmazonTrack;

            _purchaseAmazonAlbum.Visibility = showAmazonAlbum
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Hidden;
            _purchaseAmazonTrack.Visibility = showAmazonTrack
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Hidden;

            _purchaseMenu.Visibility = showPurchase
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Hidden;

            _songMenu.IsOpen = true;
        }

        private void btnMenu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _currMenuSong = GetItemSong(sender);
            ShowMenu(sender);
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowWait(false);
        }

        private void mnuTired_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuSong == null) return;
            _player.SongTired(_currMenuSong);
            if (_currMenuSong == _player.CurrentSong)
                _player.Next();
        }

        public void TiredOfCurrentSongFromSystemTray()
        {
            _player.SongTired(_player.CurrentSong);
            _player.Next();
        }

        private void mnuBookArtist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuSong != null)
            {
                _player.SongBookmarkArtist(_currMenuSong);
            }
        }

        private void mnuBookSong_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuSong != null)
            {
                _player.SongBookmark(_currMenuSong);
            }
        }

        private void SongMenu_Closed(object sender, System.Windows.RoutedEventArgs e)
        {
            _currMenuSong = null;
        }

        private void mnuCreateArtist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuSong != null)
            {
                _player.CreateStationFromArtist(_currMenuSong);
            }
        }

        private void mnuCreateSong_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuSong != null)
            {
                _player.CreateStationFromSong(_currMenuSong);
            }
        }

        private void LaunchAmazonURL(string ID)
        {
            if (ID != string.Empty)
            {
                string url = @"http://www.amazon.com/dp/" + ID;
#if APP_RELEASE
                if (ReleaseData.AmazonTag != string.Empty)
                {
                    url += (@"/?tag=" + ReleaseData.AmazonTag);
                }
#endif

                System.Diagnostics.Process.Start(url);
            }
        }

        private void mnuPurchaseAmazonAlbum_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuSong != null)
            {
                if (_currMenuSong.AmazonAlbumId != null)
                {
                    LaunchAmazonURL(_currMenuSong.AmazonAlbumId);
                }
                else
                {
                    if (_currMenuSong.AmazonAlbumUrl != null)
                    {
                        string url = _currMenuSong.AmazonAlbumUrl;

#if APP_RELEASE
                        if (ReleaseData.AmazonTag != string.Empty)
                        {
                            string oldTag = url.Substring(url.IndexOf("tag="));
                            url = url.Replace(oldTag, ReleaseData.AmazonTag);
                        }
#endif
                        System.Diagnostics.Process.Start(url);
                    }
                }
            }
        }

        private void mnuPurchaeAmazonTrack_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currMenuSong != null)
            {
                LaunchAmazonURL(_currMenuSong.AmazonTrackId);
            }
        }
    }
}