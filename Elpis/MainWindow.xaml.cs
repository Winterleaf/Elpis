/*

 *
 * * Copyright 2012 - Adam Haile
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

using Elpis.Wpf.BorderlessWindow;
using Elpis.Wpf.Pages;
using Elpis.Wpf.PageTransition;
using Elpis.Wpf.UpdateSystem;
using Enumerable = System.Linq.Enumerable;
using StringExtensions = Elpis.PandoraSharp.StringExtensions;

namespace Elpis.Wpf
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            //ContentBackground.Background.Opacity = 1.0;
            new WindowResizer(this,
                new WindowBorder(BorderPosition.TopLeft, topLeft),
                new WindowBorder(BorderPosition.Top, top),
                new WindowBorder(BorderPosition.TopRight, topRight),
                new WindowBorder(BorderPosition.Right, right),
                new WindowBorder(BorderPosition.BottomRight, bottomRight),
                new WindowBorder(BorderPosition.Bottom, bottom),
                new WindowBorder(BorderPosition.BottomLeft, bottomLeft),
                new WindowBorder(BorderPosition.Left, left));

            TitleBar.MouseLeftButtonDown += (o, e) => DragMove();
            MinimizeButton.MouseLeftButtonDown += (o, e) => WindowState = System.Windows.WindowState.Minimized;
            CloseButton.MouseLeftButtonDown += (o, e) => Close();

            _errorPage = new ErrorPage();
            _errorPage.ErrorClose += _errorPage_ErrorClose;
            transitionControl.AddPage(_errorPage);

            _loadingPage = new LoadingPage();
            transitionControl.AddPage(_loadingPage);

            _update = new UpdateCheck();

            transitionControl.ShowPage(_loadingPage);

            _config = new Config(ConfigLocation ?? "");

            if (!_config.LoadConfig())
            {
                _configError = true;
            }
            else
            {
                if (_config.Fields.Proxy_Address != string.Empty)
                    Util.PRequest.SetProxy(_config.Fields.Proxy_Address, _config.Fields.Proxy_Port,
                        _config.Fields.Proxy_User, _config.Fields.Proxy_Password);

                System.Windows.Point loc = _config.Fields.Elpis_StartupLocation;
                System.Windows.Size size = _config.Fields.Elpis_StartupSize;

                if (System.Math.Abs(loc.X - (-1)) > .0001 && System.Math.Abs(loc.Y - (-1)) > .0001)
                {
                    // Bug Fix: Issue #54, make sure that the initial window location is
                    // always fully within the virtual screen bounds.
                    // Unfortunately may not preserve window location when primary display is not left most
                    // but it eliminates the missing window problem in most situations.
                    Left = System.Math.Max(0,
                        System.Math.Min(loc.X, System.Windows.SystemParameters.VirtualScreenWidth - ActualWidth));
                    Top = System.Math.Max(0,
                        System.Math.Min(loc.Y, System.Windows.SystemParameters.VirtualScreenHeight - ActualHeight));
                }

                if (System.Math.Abs(size.Width) > .0001 && System.Math.Abs(size.Height) > .0001)
                {
                    Width = size.Width;
                    Height = size.Height;
                }
            }

            _mainWindow = this;
        }

        public static CommandLineOptions Clo;

        public static void SetCommandLine(CommandLineOptions clo)
        {
            Clo = clo;
        }

        public void DoCommandLine()
        {
            if (Clo.SkipTrack)
            {
                SkipTrack(null, null);
            }

            if (Clo.TogglePlayPause)
            {
                PlayPauseToggled(null, null);
            }

            if (Clo.DoThumbsUp)
            {
                ExecuteThumbsUp(null, null);
            }

            if (Clo.DoThumbsDown)
            {
                ExecuteThumbsDown(null, null);
            }

            if (Clo.StationToLoad != null)
            {
                LoadStation(Clo.StationToLoad);
            }
        }

        private static void ShowHelp(Util.OptionSet p)
        {
            System.Console.WriteLine(@"Usage: Elpis [OPTIONS]");
            System.Console.WriteLine(@"Greet a list of individuals with an optional message.");
            System.Console.WriteLine(@"If no message is specified, a generic greeting is used.");
            System.Console.WriteLine();
            System.Console.WriteLine(@"Options:");
            p.WriteOptionDescriptions(System.Console.Out);
        }

        protected override void OnActivated(System.EventArgs e)
        {
            _isActiveWindow = true;
            base.OnActivated(e);
        }

        protected override void OnDeactivated(System.EventArgs e)
        {
            _isActiveWindow = false;
            base.OnDeactivated(e);
        }

        #region Globals

        private readonly ErrorPage _errorPage;
        private HotKeyHost _keyHost;
        private readonly LoadingPage _loadingPage;

        private readonly System.Windows.Forms.ToolStripSeparator _notifyMenuBreakSong =
            new System.Windows.Forms.ToolStripSeparator();

        private readonly System.Windows.Forms.ToolStripSeparator _notifyMenuBreakStation =
            new System.Windows.Forms.ToolStripSeparator();

        private readonly System.Windows.Forms.ToolStripSeparator _notifyMenuBreakVote =
            new System.Windows.Forms.ToolStripSeparator();

        private readonly System.Windows.Forms.ToolStripSeparator _notifyMenuBreakExit =
            new System.Windows.Forms.ToolStripSeparator();

        private About _aboutPage;

        private readonly Config _config;

        private bool _finalComplete;
        private bool _initComplete;
        private LoginPage _loginPage;

        private System.Windows.Forms.NotifyIcon _notify;
        private System.Windows.Forms.ContextMenuStrip _notifyMenu;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuAlbum;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuArtist;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuNext;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuPlayPause;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuStations;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuTitle;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuUpVote;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuDownVote;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuTired;
        private System.Windows.Forms.ToolStripMenuItem _notifyMenuExit;
        private System.Threading.Timer _notifyDoubleClickTimer;
        private static bool _notifyDoubleClicked;
        public static PandoraSharpPlayer.Player Player;
        public static PlaylistPage PlaylistPage;
        private static MainWindow _mainWindow;
        private System.Windows.Controls.UserControl _prevPage;
        private Search _searchPage;
        private Settings _settingsPage;
        private bool _showingError;
        private bool _stationLoaded;

        private SearchMode _searchMode = SearchMode.NewStation;

        private readonly bool _configError;

        private bool _forceClose;

        private StationList _stationPage;
        private QuickMixPage _quickMixPage;
        private readonly UpdateCheck _update;
#pragma warning disable 169
        private UpdatePage _updatePage;
#pragma warning restore 169
        private RestartPage _restartPage;
        private LastFmAuthPage _lastFmPage;

        private Util.ErrorCodes _lastError = Util.ErrorCodes.Success;
        private System.Exception _lastException;

        private PandoraSharpScrobbler.PandoraSharpScrobbler _scrobbler;

        private bool _isActiveWindow;

        private static System.DateTime _lastTimeSkipped;

        private WebInterface _webInterfaceObject;

        private bool _restarting;

        private const int PLAY = 1;
        private const int PAUSE = 2;
        private const int LIKE = 3;
        private const int DISLIKE = 4;
        private const int SKIP = 5;

        #endregion

        #region Release Data Values

        private string _bassRegEmail = "";
        private string _bassRegKey = "";

        public string ConfigLocation { get; set; }

        public string StartupStation { get; set; } = null;

        public void InitReleaseData()
        {
            _bassRegEmail = "";
            _bassRegKey = "";

#if APP_RELEASE
            _bassRegEmail = ReleaseData.BassRegEmail;
            _bassRegKey = ReleaseData.BassRegKey;
#endif
        }

        #endregion

        #region Setups

        private void SetupLogging()
        {
            if (_config.Fields.Debug_WriteLog)
            {
                _loadingPage.UpdateStatus("Initializing logging...");
                string logFilename = "elpis{0}.log";
                logFilename = string.Format(logFilename, _config.Fields.Debug_Timestamp ? System.DateTime.Now.ToString("_MMdd-hhmmss") : "");

                string path = System.IO.Path.Combine(_config.Fields.Debug_Logpath, logFilename);

                if (!System.IO.Directory.Exists(_config.Fields.Debug_Logpath))
                    System.IO.Directory.CreateDirectory(_config.Fields.Debug_Logpath);

                Util.Log.SetLogPath(path);
            }
        }

        private void CloseSettings()
        {
            _scrobbler.IsEnabled = _config.Fields.LastFM_Scrobble;
            RestorePrevPage();
        }

        private void SetupPageEvents()
        {
            _settingsPage.Close += CloseSettings;
            _settingsPage.Restart += _settingsPage_Restart;
            _settingsPage.LastFmAuthRequest += _settingsPage_LastFMAuthRequest;
            _settingsPage.LasFmDeAuthRequest += _settingsPage_LasFMDeAuthRequest;
            _restartPage.RestartSelectionEvent += _restartPage_RestartSelectionEvent;
            _lastFmPage.ContinueEvent += _lastFMPage_ContinueEvent;
            _lastFmPage.CancelEvent += _lastFMPage_CancelEvent;
            _aboutPage.Close += RestorePrevPage;

            _searchPage.Cancel += _searchPage_Cancel;
            _searchPage.AddVariety += _searchPage_AddVariety;
            _loginPage.ConnectingEvent += _loginPage_ConnectingEvent;
        }

        private void _settingsPage_LasFMDeAuthRequest()
        {
            _config.Fields.LastFM_SessionKey = string.Empty;
            _config.Fields.LastFM_Scrobble = false;
            _config.SaveConfig();
        }

        private void _settingsPage_LastFMAuthRequest()
        {
            this.BeginDispatch(() =>
            {
                try
                {
                    string url = _scrobbler.GetAuthUrl();
                    _lastFmPage.SetAuthUrl(url);
                    _scrobbler.LaunchAuthPage();

                    transitionControl.ShowPage(_lastFmPage);
                }
                catch (System.Exception ex)
                {
                    ShowError(Util.ErrorCodes.ErrorGettingToken, ex);
                }
            });
        }

        private void _lastFMPage_CancelEvent()
        {
            transitionControl.ShowPage(_settingsPage);
        }

        private void _lastFMPage_ContinueEvent()
        {
            this.Dispatch(GetLastFmSessionKey);
        }

        private void _settingsPage_Restart()
        {
            transitionControl.ShowPage(_restartPage);
        }

        private void DoRestart()
        {
            string[] cmds = System.Environment.GetCommandLineArgs();
            System.Collections.Generic.List<string> args = Enumerable.ToList(cmds);

            args.RemoveAt(0);
            args.Remove("-restart");

            string sArgs = Enumerable.Aggregate(args, string.Empty, (current, s) => current + (s + " "));

            sArgs += " -restart";

            System.Diagnostics.Process.Start("Elpis.exe", sArgs);
        }

        private void _restartPage_RestartSelectionEvent(bool status)
        {
            if (status)
            {
                _restarting = true;
                DoRestart();
                Close();
            }
            else
            {
                RestorePrevPage();
            }
        }

        private System.DateTime _lastFmStart;
        private bool _lastFmAuth;

        private void DoLastFmAuth()
        {
            try
            {
                _lastFmStart = System.DateTime.Now;
                while ((System.DateTime.Now - _lastFmStart).TotalMilliseconds < 5000) System.Threading.Thread.Sleep(10);

                string sk = _scrobbler.GetAuthSessionKey();
                _config.Fields.LastFM_Scrobble = true;
                _config.Fields.LastFM_SessionKey = sk;
                _config.SaveConfig();

                DoLastFmSuccess();
            }
            catch (System.Exception ex)
            {
                _config.Fields.LastFM_Scrobble = false;
                _config.Fields.LastFM_SessionKey = string.Empty;
                _config.SaveConfig();

                DoLastFmError(ex);
            }
        }

        private void DoLastFmSuccess()
        {
            _lastFmStart = System.DateTime.Now;
            this.BeginDispatch(() => _loadingPage.UpdateStatus("Success!"));
            while ((System.DateTime.Now - _lastFmStart).TotalMilliseconds < 1500) System.Threading.Thread.Sleep(10);
            this.BeginDispatch(() => transitionControl.ShowPage(_settingsPage));
            _lastFmAuth = false;
        }

        private void DoLastFmError(System.Exception ex)
        {
            _lastFmStart = System.DateTime.Now;
            this.BeginDispatch(() =>
            {
                _lastError = Util.ErrorCodes.ErrorGettingSession;
                //ShowError(_lastError, ex);
                _loadingPage.UpdateStatus("Error Fetching Last.FM Session");
            });
            while ((System.DateTime.Now - _lastFmStart).TotalMilliseconds < 3000) System.Threading.Thread.Sleep(10);
            this.BeginDispatch(() => transitionControl.ShowPage(_settingsPage));
            _lastFmAuth = false;
        }

        private void GetLastFmSessionKey()
        {
            _lastFmAuth = true;
            _lastFmStart = System.DateTime.Now;
            _loadingPage.UpdateStatus("Fetching Last.FM Session");
            transitionControl.ShowPage(_loadingPage);

            System.Threading.Tasks.Task.Factory.StartNew(DoLastFmAuth);
        }

        private void SetupUiEvents()
        {
            Player.ConnectionEvent += _player_ConnectionEvent;
            Player.LogoutEvent += _player_LogoutEvent;
            Player.StationLoaded += _player_StationLoaded;
            Player.StationsRefreshed += _player_StationsRefreshed;
            Player.StationsRefreshing += _player_StationsRefreshing;
            Player.ExceptionEvent += _player_ExceptionEvent;
            Player.PlaybackStateChanged += _player_PlaybackStateChanged;
            Player.LoginStatusEvent += _player_LoginStatusEvent;
            Player.PlaybackStart += _player_PlaybackStart;
            Player.StationCreated += _player_StationCreated;

            mainBar.PlayPauseClick += mainBar_PlayPauseClick;
            mainBar.NextClick += mainBar_NextClick;
            mainBar.AboutClick += mainBar_AboutClick;
            mainBar.SettingsClick += mainBar_SettingsClick;
            mainBar.StationListClick += mainBar_stationPageClick;
            mainBar.CreateStationClick += mainBar_searchPageClick;
            mainBar.ErrorClicked += mainBar_ErrorClicked;
            mainBar.VolumeChanged += mainBar_VolumeChanged;

            _loginPage.Loaded += _loginPage_Loaded;
            _aboutPage.Loaded += _aboutPage_Loaded;
            _settingsPage.Loaded += _settingsPage_Loaded;
            _settingsPage.Logout += _settingsPage_Logout;
            _searchPage.Loaded += _searchPage_Loaded;
            _stationPage.Loaded += _stationPage_Loaded;
            _stationPage.EditQuickMixEvent += _stationPage_EditQuickMixEvent;
            _stationPage.AddVarietyEvent += _stationPage_AddVarietyEvent;
            _quickMixPage.CancelEvent += _quickMixPage_CancelEvent;
            _quickMixPage.CloseEvent += _quickMixPage_CloseEvent;
            PlaylistPage.Loaded += _playlistPage_Loaded;
        }

        private void SetupPages()
        {
            _searchPage = new Search(Player);
            transitionControl.AddPage(_searchPage);

            _settingsPage = new Settings(Player, _config, _keyHost);
            transitionControl.AddPage(_settingsPage);

            _restartPage = new RestartPage();
            transitionControl.AddPage(_restartPage);

            _aboutPage = new About();
            transitionControl.AddPage(_aboutPage);

            _stationPage = new StationList(Player);
            transitionControl.AddPage(_stationPage);

            _quickMixPage = new QuickMixPage(Player);
            transitionControl.AddPage(_quickMixPage);

            _loginPage = new LoginPage(Player, _config);
            transitionControl.AddPage(_loginPage);

            PlaylistPage = new PlaylistPage(Player);
            transitionControl.AddPage(PlaylistPage);

            _lastFmPage = new LastFmAuthPage();
            transitionControl.AddPage(_lastFmPage);
        }

        private static void StationMenuClick(object sender, System.EventArgs e)
        {
            PandoraSharp.Station station = (PandoraSharp.Station) ((System.Windows.Forms.ToolStripMenuItem) sender).Tag;
            Player.PlayStation(station);
        }

        private void AddStationMenuItems()
        {
            if (_notify == null || _notifyMenu == null || Player.Stations.Count <= 0) return;
            _notifyMenuStations.DropDown.Items.Clear();
            foreach (PandoraSharp.Station s in Player.Stations)
            {
                System.Windows.Forms.ToolStripMenuItem menu = new System.Windows.Forms.ToolStripMenuItem(s.Name);
                menu.Click += StationMenuClick;
                menu.Tag = s;
                _notifyMenuStations.DropDown.Items.Add(menu);
            }
        }

        private static void LoadNotifyDetailUrl(object sender, System.EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start((string) ((System.Windows.Forms.ToolStripMenuItem) sender).Tag);
            }
            catch
            {
                //todo
            }
        }

        private void LoadNotifyMenu()
        {
            bool showSongInfo = !Player.Stopped;
            bool showStations = false;
            if (Player.Stations != null)
                showStations = Player.Stations.Count > 0;

            _notifyMenuTitle.Visible =
                _notifyMenuArtist.Visible =
                    _notifyMenuAlbum.Visible =
                        _notifyMenuBreakSong.Visible =
                            _notifyMenuDownVote.Visible =
                                _notifyMenuUpVote.Visible =
                                    _notifyMenuTired.Visible = _notifyMenuBreakVote.Visible = showSongInfo;

            _notifyMenuPlayPause.Enabled = _notifyMenuNext.Enabled = showSongInfo;

            if (showSongInfo)
            {
                _notifyMenuTitle.Text = Player.CurrentSong.SongTitle.Replace("&", "&&&");
                _notifyMenuTitle.Tag = Player.CurrentSong.SongDetailUrl;

                _notifyMenuArtist.Text = @"by " + Player.CurrentSong.Artist.Replace("&", "&&&");
                _notifyMenuArtist.Tag = Player.CurrentSong.ArtistDetailUrl;

                _notifyMenuAlbum.Text = @"on " + Player.CurrentSong.Album.Replace("&", "&&&");
                _notifyMenuAlbum.Tag = Player.CurrentSong.AlbumDetailUrl;

                _notifyMenuPlayPause.Text = Player.Playing ? "Pause" : "Play";
            }

            _notifyMenuBreakStation.Visible = _notifyMenuStations.Visible = showStations;

            _notifyMenuBreakExit.Visible = _notifyMenuExit.Visible = true;

            if (showStations)
                AddStationMenuItems();
        }

        private void SetupNotifyIcon()
        {
            _notifyMenuTitle = new System.Windows.Forms.ToolStripMenuItem("Title");
            _notifyMenuTitle.Click += LoadNotifyDetailUrl;
            _notifyMenuTitle.Image = Properties.Resources.menu_info;

            _notifyMenuArtist = new System.Windows.Forms.ToolStripMenuItem("Artist");
            _notifyMenuArtist.Click += LoadNotifyDetailUrl;
            _notifyMenuArtist.Image = Properties.Resources.menu_info;

            _notifyMenuAlbum = new System.Windows.Forms.ToolStripMenuItem("Album");
            _notifyMenuAlbum.Click += LoadNotifyDetailUrl;
            _notifyMenuAlbum.Image = Properties.Resources.menu_info;

            _notifyMenuPlayPause = new System.Windows.Forms.ToolStripMenuItem("Play");
            _notifyMenuPlayPause.Click += (o, e) => Player.PlayPause();

            _notifyMenuNext = new System.Windows.Forms.ToolStripMenuItem("Next Song");
            _notifyMenuNext.Click += (o, e) => Player.Next();

            _notifyMenuStations = new System.Windows.Forms.ToolStripMenuItem("Stations");

            _notifyMenuDownVote = new System.Windows.Forms.ToolStripMenuItem("Dislike Song");
            _notifyMenuDownVote.Click += (o, e) => PlaylistPage.ThumbDownCurrent();

            _notifyMenuTired = new System.Windows.Forms.ToolStripMenuItem("Tired of This Song");
            _notifyMenuTired.Click += (o, e) => PlaylistPage.TiredOfCurrentSongFromSystemTray();

            _notifyMenuUpVote = new System.Windows.Forms.ToolStripMenuItem("Like Song");
            _notifyMenuUpVote.Click += (o, e) => PlaylistPage.ThumbUpCurrent();

            _notifyMenuExit = new System.Windows.Forms.ToolStripMenuItem("Exit Elpis");
            _notifyMenuExit.Click += (o, e) =>
            {
                _forceClose = true;
                Close();
            };

            System.Windows.Forms.ToolStripItem[] menus =
            {
                _notifyMenuTitle, _notifyMenuArtist, _notifyMenuAlbum,
                _notifyMenuBreakSong, _notifyMenuPlayPause, _notifyMenuNext, _notifyMenuBreakVote,
                _notifyMenuUpVote, _notifyMenuDownVote, _notifyMenuTired, _notifyMenuBreakStation,
                _notifyMenuStations, _notifyMenuBreakExit, _notifyMenuExit
            };

            _notifyMenu = new System.Windows.Forms.ContextMenuStrip();
            _notifyMenu.Items.AddRange(menus);

            _notify = new System.Windows.Forms.NotifyIcon
            {
                Text = @"Elpis",
                Icon = Properties.Resources.main_icon,
                ContextMenuStrip = _notifyMenu
            };

            // Timer is used to distinguish between mouse single and double clicks
            _notifyDoubleClickTimer = new System.Threading.Timer(o =>
            {
                System.Threading.Thread.Sleep(System.Windows.Forms.SystemInformation.DoubleClickTime);
                if (!_notifyDoubleClicked)
                {
                    Player.PlayPause();
                }
                _notifyDoubleClicked = false;
            });

            _notify.MouseDoubleClick += (o, e) =>
            {
                // Only process left mouse button double clicks
                if (e.Button != System.Windows.Forms.MouseButtons.Left)
                {
                    return;
                }

                _notifyDoubleClicked = true;

                // Hide window if it is shown; show if it is hidden
                if (WindowState == System.Windows.WindowState.Normal)
                {
                    WindowState = System.Windows.WindowState.Minimized;
                    Hide();
                    ShowInTaskbar = false;
                }
                else
                {
                    NativeMethods.ShowToFront(
                        new System.Windows.Interop.WindowInteropHelper(this).Handle);
                }
            };

            _notify.MouseClick += (o, e) =>
            {
                switch (e.Button) {
                    case System.Windows.Forms.MouseButtons.Left:
                        // Play or pause only in the event of single click
                        _notifyDoubleClickTimer.Change(0, 0);
                        break;
                    case System.Windows.Forms.MouseButtons.Middle:
                        Player.Next();
                        break;
                    case System.Windows.Forms.MouseButtons.None:
                        break;
                    case System.Windows.Forms.MouseButtons.Right:
                        break;
                    case System.Windows.Forms.MouseButtons.XButton1:
                        break;
                    case System.Windows.Forms.MouseButtons.XButton2:
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException();
                }
            };

            _notify.ContextMenuStrip.Opening += (o, e) => LoadNotifyMenu();

            _notify.Visible = true;
        }

        private bool InitLogic()
        {
            while (!Equals(transitionControl.CurrentPage, _loadingPage)) System.Threading.Thread.Sleep(10);
            _loadingPage.UpdateStatus("Loading configuration...");
            InitReleaseData();

            if (_configError)
            {
                this.BeginDispatch(() => ShowError(Util.ErrorCodes.ConfigLoadError, null));
                return false;
            }

            try
            {
                SetupLogging();
            }
            catch (System.Exception ex)
            {
                ShowError(Util.ErrorCodes.LogSetupError, ex);
                return false;
            }
            _initComplete = true;
            return true;
        }

        private void FinalLoad()
        {
            System.Version ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            if (_config.Fields.Elpis_Version == null || _config.Fields.Elpis_Version < ver)
            {
                _loadingPage.UpdateStatus("Running update logic...");

                string oldVer = _config.Fields.Elpis_Version.ToString();

                _config.Fields.Elpis_Version = ver;
                _config.SaveConfig();

#if APP_RELEASE
                var post = new PostSubmitter(ReleaseData.AnalyticsPostURL);

                post.Add("guid", _config.Fields.Elpis_InstallID);
                post.Add("curver", oldVer);
                post.Add("newver", _config.Fields.Elpis_Version.ToString());
                post.Add("osver", SystemInfo.GetWindowsVersion());

                try
                {
                    post.Send();
                }
                catch(Exception ex)
                {
                    Log.O(ex.ToString());
                }
#endif
            }

            _loadingPage.UpdateStatus("Loading audio engine...");
            try
            {
                Player = new PandoraSharpPlayer.Player();
                Player.Initialize(_bassRegEmail, _bassRegKey); //TODO - put this in the login sequence?
                if (_config.Fields.Proxy_Address != string.Empty)
                    Player.SetProxy(_config.Fields.Proxy_Address, _config.Fields.Proxy_Port, _config.Fields.Proxy_User, _config.Fields.Proxy_Password);
                setOutputDevice(_config.Fields.System_OutputDevice);
            }
            catch (System.Exception ex)
            {
                ShowError(Util.ErrorCodes.EngineInitError, ex);
                return;
            }

            LoadLastFm();

            Player.AudioFormat = _config.Fields.Pandora_AudioFormat;
            Player.SetStationSortOrder(_config.Fields.Pandora_StationSortOrder);
            Player.Volume = _config.Fields.Elpis_Volume;
            Player.PauseOnLock = _config.Fields.Elpis_PauseOnLock;
            Player.MaxPlayed = _config.Fields.Elpis_MaxHistory;

            if (System.IO.Directory.Exists(_config.Fields.RipPath))
            {
                Player.RipPath = _config.Fields.RipPath;
                Player.Rip = _config.Fields.RipStream;
            }
            else
            {
                Player.Rip = false;
            }

            //_player.ForceSSL = _config.Fields.Misc_ForceSSL;

            _loadingPage.UpdateStatus("Setting up cache...");
            string cachePath = System.IO.Path.Combine(Config.ElpisAppData, "Cache");
            if (!System.IO.Directory.Exists(cachePath)) System.IO.Directory.CreateDirectory(cachePath);
            Player.ImageCachePath = cachePath;

            _loadingPage.UpdateStatus("Starting Web Server...");

            StartWebServer();

            _loadingPage.UpdateStatus("Setting up UI...");

            this.Dispatch(() =>
            {
                _keyHost = new HotKeyHost(this);
                ConfigureHotKeys();
            });

            //this.Dispatch(SetupJumpList);

            this.Dispatch(SetupNotifyIcon);

            this.Dispatch(() => mainBar.DataContext = Player); //To bind playstate

            this.Dispatch(SetupPages);
            this.Dispatch(SetupUiEvents);
            this.Dispatch(SetupPageEvents);

            if (_config.Fields.Login_AutoLogin && !string.IsNullOrEmpty(_config.Fields.Login_Email) && !string.IsNullOrEmpty(_config.Fields.Login_Password))
            {
                Player.Connect(_config.Fields.Login_Email, _config.Fields.Login_Password);
            }
            else
            {
                transitionControl.ShowPage(_loginPage);
            }

            this.Dispatch(() => mainBar.Volume = Player.Volume);

            _finalComplete = true;
        }

        private void setOutputDevice(string systemOutputDevice)
        {
            if (!StringExtensions.IsNullOrEmpty(systemOutputDevice))
            {
                string prevOutput = Player.OutputDevice;
                try
                {
                    Player.OutputDevice = systemOutputDevice;
                }
                catch (BassPlayer.BassException )
                {
                    Player.OutputDevice = prevOutput;
                }
            }
        }

        private void StartWebServer()
        {
            if (_config.Fields.Elpis_RemoteControlEnabled)
            {
                _webInterfaceObject = new WebInterface();
                System.Threading.Thread webInterfaceThread = new System.Threading.Thread(_webInterfaceObject.StartInterface);
                webInterfaceThread.Start();
                _lastTimeSkipped = System.DateTime.Now;
            }
        }

        private void StopWebServer()
        {
            if (_config.Fields.Elpis_RemoteControlEnabled)
            {
                _webInterfaceObject?.StopInterface();
            }
        }

        public static bool Next()
        {
            if ((System.DateTime.Now - _lastTimeSkipped).Seconds > 20)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action) (() =>
                {
                    _mainWindow.ShowBalloon(SKIP);
                    Player.Next();
                }));
                _lastTimeSkipped = System.DateTime.Now;
                return true;
            }
            return false;
        }

        public static void Pause()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action) (() =>
            {
                _mainWindow.ShowBalloon(PAUSE);
                Player.Pause();
            }));
        }

        public static void Play()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action) (() =>
            {
                _mainWindow.ShowBalloon(PLAY);
                Player.Play();
            }));
        }

        public static void PlayPauseToggle()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action) (() =>
            {
                if (Player.Paused)
                {
                    _mainWindow.ShowBalloon(PLAY);
                }
                if (Player.Playing)
                {
                    _mainWindow.ShowBalloon(PAUSE);
                }
                Player.PlayPause();
            }));
        }

        public static void Like()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action) (() =>
            {
                _mainWindow.ShowBalloon(LIKE);
                PlaylistPage.ThumbUpCurrent();
            }));
        }

        public static void Dislike()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action) (() =>
            {
                _mainWindow.ShowBalloon(DISLIKE);
                PlaylistPage.ThumbDownCurrent();
            }));
        }

        public static PandoraSharp.Song GetCurrentSong()
        {
            return Player.CurrentSong;
        }

        private void LoadLastFm()
        {
            string apiKey;
            string apiSecret;
#if APP_RELEASE
                apiKey = ReleaseData.LastFMApiKey;
                apiSecret = ReleaseData.LastFMApiSecret;
#else
            //Put your own Last.FM API keys here
            apiKey = "dummy_key";
            apiSecret = "dummy_key";
#endif

            _scrobbler = !string.IsNullOrEmpty(_config.Fields.LastFM_SessionKey) ? new PandoraSharpScrobbler.PandoraSharpScrobbler(apiKey, apiSecret, _config.Fields.LastFM_SessionKey) : new PandoraSharpScrobbler.PandoraSharpScrobbler(apiKey, apiSecret);

            _scrobbler.IsEnabled = _config.Fields.LastFM_Scrobble;
#if APP_RELEASE
#else
            if (_config.Fields.LastFM_Scrobble && !_scrobbler.IsEnabled)
            {
                System.Windows.MessageBox.Show("You are trying to use Last.FM Scrobbler without a LastFM API key. " + "In order to use it while in Debug mode, edit apiKey and apiSecret in LoadLastFM() in MainWindow.xaml.cs");
            }
#endif

            if (_config.Fields.Proxy_Address != string.Empty)
                _scrobbler.SetProxy(_config.Fields.Proxy_Address, _config.Fields.Proxy_Port, _config.Fields.Proxy_User, _config.Fields.Proxy_Password);

            Player.RegisterPlayerControlQuery(_scrobbler);
        }

        private void LoadLogic()
        {
            bool foundNewUpdate = false;
            if (InitLogic())
            {
#if APP_RELEASE
                _update = new UpdateCheck();
                if (_config.Fields.Elpis_CheckUpdates)
                {
                    _loadingPage.UpdateStatus("Checking for updates...");
                    if (_update.CheckForUpdate())
                    {
                        foundNewUpdate = true;
                        this.BeginDispatch(() =>
                                               {
                                                   _updatePage = new UpdatePage(_update);
                                                   _updatePage.UpdateSelectionEvent += _updatePage_UpdateSelectionEvent;
                                                   transitionControl.AddPage(_updatePage);
                                                   transitionControl.ShowPage(_updatePage);
                                               });
                    }
                }
                if (_config.Fields.Elpis_CheckBetaUpdates && !foundNewUpdate)
                {
                    _loadingPage.UpdateStatus("Checking for Beta updates...");
                    if (_update.CheckForBetaUpdate())
                    {
                        foundNewUpdate = true;
                        this.BeginDispatch(() =>
                        {
                            _updatePage = new UpdatePage(_update);
                            _updatePage.UpdateSelectionEvent += _updatePage_UpdateSelectionEvent;
                            transitionControl.AddPage(_updatePage);
                            transitionControl.ShowPage(_updatePage);
                        });
                    }
                }
                if (_config.Fields.Elpis_CheckBetaUpdates || _config.Fields.Elpis_CheckUpdates)
                {
                    if (!foundNewUpdate)
                    {
                        FinalLoad();
                    }
                }
                else
                {
                    FinalLoad();
                }
#else
                FinalLoad();
#endif
            }
        }

        #endregion

        #region Misc Methods

        private bool IsOnPlaylist()
        {
            return IsActive && Equals(transitionControl.CurrentPage, PlaylistPage);
        }

        private void SetupJumpList()
        {
            //JumpList jumpList = new JumpList();
            //jumpList.ShowRecentCategory = true;
            //JumpList.SetJumpList(System.Windows.Application.Current, jumpList);

            //JumpTask pause = JumpListManager.createJumpTask(PlayerCommands.PlayPause, "--playpause",1);
            //jumpList.JumpItems.Add(pause);

            //JumpTask next = JumpListManager.createJumpTask(PlayerCommands.Next, "--next",2);
            //jumpList.JumpItems.Add(next);

            //JumpTask thumbsUp  = JumpListManager.createJumpTask(PlayerCommands.ThumbsUp, "--thumbsup",3);
            //jumpList.JumpItems.Add(thumbsUp);

            //JumpTask thumbsDown = JumpListManager.createJumpTask(PlayerCommands.ThumbsDown, "--thumbsdown",4);
            //jumpList.JumpItems.Add(thumbsDown);

            //jumpList.Apply();
        }

        private void ConfigureHotKeys()
        {
            foreach (HotkeyConfig h in _config.Fields.Elpis_HotKeys.Values)
            {
                _keyHost.AddHotKey(h);
            }
            if (new System.Collections.Generic.List<HotKey>(_config.Fields.Elpis_HotKeys.Values).Count == 0)
            {
                _keyHost.AddHotKey(new HotKey(PlayerCommands.PlayPause, System.Windows.Input.Key.MediaPlayPause, System.Windows.Input.ModifierKeys.None, true));

                _keyHost.AddHotKey(new HotKey(PlayerCommands.Next, System.Windows.Input.Key.MediaNextTrack, System.Windows.Input.ModifierKeys.None, true));

                _keyHost.AddHotKey(new HotKey(PlayerCommands.ThumbsUp, System.Windows.Input.Key.MediaPlayPause, System.Windows.Input.ModifierKeys.Control, true));

                _keyHost.AddHotKey(new HotKey(PlayerCommands.ThumbsDown, System.Windows.Input.Key.MediaStop, System.Windows.Input.ModifierKeys.Control, true));
            }

            System.Collections.Generic.Dictionary<int, HotkeyConfig> keys = new System.Collections.Generic.Dictionary<int, HotkeyConfig>();
            foreach (System.Collections.Generic.KeyValuePair<int, HotKey> pair in _keyHost.HotKeys)
            {
                keys.Add(pair.Key, new HotkeyConfig(pair.Value));
            }
            _config.Fields.Elpis_HotKeys = keys;

            _config.SaveConfig();
        }

        public void ShowStationList()
        {
            _stationPage.Stations = Player.Stations;
            transitionControl.ShowPage(_stationPage, PageTransitionType.Next);
        }

        private void RestorePrevPage()
        {
            transitionControl.ShowPage(_prevPage, PageTransitionType.Next);
            _prevPage = null;
        }

        private void ShowErrorPage(Util.ErrorCodes code, System.Exception ex)
        {
            if (!_showingError)
            {
                _showingError = true;

                _prevPage = transitionControl.CurrentPage;
                _errorPage.SetError(Util.Errors.GetErrorMessage(code), Util.Errors.IsHardFail(code), ex);
                transitionControl.ShowPage(_errorPage);
            }
        }

        private void ShowError(Util.ErrorCodes code, System.Exception ex, bool showLast = false)
        {
            if (!Equals(transitionControl.CurrentPage, _errorPage))
            {
                if (showLast && _lastError != Util.ErrorCodes.Success)
                {
                    ShowErrorPage(_lastError, _lastException);
                }
                else if (code != Util.ErrorCodes.Success && ex != null)
                {
                    if (Util.Errors.IsHardFail(code))
                    {
                        ShowErrorPage(code, ex);
                    }
                    else
                    {
                        _lastError = code;
                        _lastException = ex;
                        mainBar.ShowError(Util.Errors.GetErrorMessage(code));

                        if (!Equals(transitionControl.CurrentPage, _loadingPage) || _lastFmAuth) return;
                        _loginPage.LoginFailed = true;
                        transitionControl.ShowPage(_loginPage);
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private void mainBar_ErrorClicked()
        {
            ShowError(Util.ErrorCodes.Success, null, true);
        }

        private void _player_StationsRefreshing(object sender)
        {
            _stationPage.SetStationsRefreshing();
        }

        private void _loginPage_ConnectingEvent()
        {
            this.BeginDispatch(() => transitionControl.ShowPage(_loadingPage, PageTransitionType.Next));
        }

        private void _errorPage_ErrorClose(bool hardFail)
        {
            if (hardFail || _prevPage == null)
                Close();
            else
            {
                _lastError = Util.ErrorCodes.Success;
                _lastException = null;
                RestorePrevPage();
                _showingError = false;
            }
        }

        private void _updatePage_UpdateSelectionEvent(bool status)
        {
            if (status && _update.DownloadUrl != string.Empty)
            {
                //Process.Start(_update.DownloadUrl);
                Close();
            }
            else
            {
                transitionControl.ShowPage(_loadingPage);
            }
        }

        private void _player_StationCreated(object sender, PandoraSharp.Station station)
        {
            Player.RefreshStations();
            this.BeginDispatch(() => Player.PlayStation(station));
        }

        private void _searchPage_Cancel(object sender)
        {
            this.BeginDispatch(() =>
            {
                if (_searchMode == SearchMode.AddVariety)
                    ShowStationList();
                else
                {
                    if (Equals(_prevPage, _stationPage))
                        ShowStationList();
                    else
                        RestorePrevPage();
                    //transitionControl.ShowPage(_playlistPage);
                }
            });
        }

        private void _searchPage_AddVariety(object sender)
        {
            ShowStationList();
        }

        private void _player_PlaybackStart(object sender, double duration)
        {
            this.BeginDispatch(() => { ShowBalloon(PLAY, 5000); });
        }

        private void _player_PlaybackStateChanged(object sender, BassPlayer.BassAudioEngine.PlayState oldState, BassPlayer.BassAudioEngine.PlayState newState)
        {
            this.BeginDispatch(() =>
            {
                switch (newState) {
                    case BassPlayer.BassAudioEngine.PlayState.Playing:
                        string title = "Elpis | " + Player.CurrentSong.Artist + " / " + Player.CurrentSong.SongTitle;

                        _notify.Text = Util.StringExtensions.StringEllipses(title.Replace("&", "&&&"), 63);
                        //notify text cannot be more than 63 chars
                        Title = title;
                        break;
                    case BassPlayer.BassAudioEngine.PlayState.Paused:
                        Title = _notify.Text = @"Elpis";
                        break;
                    case BassPlayer.BassAudioEngine.PlayState.Stopped:
                        mainBar.SetPlaying(false);
                        Title = _notify.Text = @"Elpis";
                        if (Player.LoggedIn)
                            ShowStationList();
                        break;
                    case BassPlayer.BassAudioEngine.PlayState.Init:
                        break;
                    case BassPlayer.BassAudioEngine.PlayState.Ended:
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(newState), newState, null);
                }
            });
        }

        private void _player_ExceptionEvent(object sender, Util.ErrorCodes code, System.Exception ex)
        {
            ShowError(code, ex);
        }

        private void _player_LoginStatusEvent(object sender, string status)
        {
            _loadingPage.UpdateStatus(status);
        }

        private void _stationPage_EditQuickMixEvent()
        {
            transitionControl.ShowPage(_quickMixPage);
        }

        private void _stationPage_AddVarietyEvent(PandoraSharp.Station station)
        {
            _searchPage.SearchMode = _searchMode = SearchMode.AddVariety;
            _searchPage.VarietyStation = station;
            transitionControl.ShowPage(_searchPage);
        }

        private void _quickMixPage_CloseEvent()
        {
            ShowStationList();
        }

        private void _quickMixPage_CancelEvent()
        {
            ShowStationList();
        }

        private void _playlistPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Util.Log.O("Show Playlist");
            mainBar.SetModePlayList();
        }

        private void _stationPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Util.Log.O("Show Stations");
            mainBar.SetModeStationList(Player.CurrentStation != null);
        }

        private void _searchPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Util.Log.O("Show Search");
            mainBar.SetModeSearch();
        }

        private void _settingsPage_Logout()
        {
            if (Player.LoggedIn)
                Player.Logout();
        }

        private void _settingsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Util.Log.O("Show Settings");
            mainBar.SetModeSettings();
        }

        private void _aboutPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Util.Log.O("Show About");
            mainBar.SetModeAbout();
        }

        private void _loginPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Util.Log.O("Show Login");
            mainBar.SetModeLogin();
        }

        private void mainBar_stationPageClick()
        {
            //_prevPage = transitionControl.CurrentPage;
            if (Equals(transitionControl.CurrentPage, _stationPage) || Equals(transitionControl.CurrentPage, _quickMixPage))
            {
                if (_stationLoaded)
                    transitionControl.ShowPage(PlaylistPage);
            }
            else
            {
                transitionControl.ShowPage(_stationPage);
            }
        }

        private void mainBar_searchPageClick()
        {
            _prevPage = transitionControl.CurrentPage;
            _searchPage.SearchMode = _searchMode = SearchMode.NewStation;
            transitionControl.ShowPage(_searchPage, PageTransitionType.Previous);
        }

        private static void mainBar_VolumeChanged(double vol)
        {
            Player.Volume = (int) vol;
        }

        private void mainBar_SettingsClick()
        {
            if (_prevPage == null)
                _prevPage = transitionControl.CurrentPage;

            if (Equals(transitionControl.CurrentPage, _settingsPage))
                RestorePrevPage();
            else
                transitionControl.ShowPage(_settingsPage, PageTransitionType.Previous);
        }

        private void mainBar_AboutClick()
        {
            if (_prevPage == null)
                _prevPage = transitionControl.CurrentPage;

            if (Equals(transitionControl.CurrentPage, _aboutPage))
                RestorePrevPage();
            else
                transitionControl.ShowPage(_aboutPage, PageTransitionType.Previous);
        }

        private void _player_StationsRefreshed(object sender)
        {
            _stationPage.Stations = Player.Stations;
        }

        private void mainBar_NextClick()
        {
            //if (transitionControl.CurrentPage == _playlistPage)
            Player.Next();

            transitionControl.ShowPage(PlaylistPage);
        }

        private void mainBar_PlayPauseClick()
        {
            //if (transitionControl.CurrentPage == _playlistPage)
            Player.PlayPause();

            transitionControl.ShowPage(PlaylistPage);
        }

        private void _player_StationLoaded(object sender, PandoraSharp.Station station)
        {
            this.BeginDispatch(() =>
            {
                mainBar.SetModePlayList();
                transitionControl.ShowPage(PlaylistPage, PageTransitionType.Next);
                if (_config.Fields.Pandora_AutoPlay)
                {
                    _config.Fields.Pandora_LastStationID = station.Id;
                    _config.SaveConfig();
                }

                _stationLoaded = true;
            });
        }

        private void _player_LogoutEvent(object sender)
        {
            transitionControl.ShowPage(_loginPage);
        }

        private void _player_ConnectionEvent(object sender, bool state, Util.ErrorCodes code)
        {
            if (state)
            {
                if (_config.Fields.Pandora_AutoPlay)
                {
                    PandoraSharp.Station s = null;
                    if (StartupStation != null)
                        s = Player.GetStationFromString(StartupStation);
                    if (s == null)
                    {
                        s = Player.GetStationFromId(_config.Fields.Pandora_LastStationID);
                    }
                    if (s != null)
                    {
                        _loadingPage.UpdateStatus("Loading Station:" + System.Environment.NewLine + s.Name);
                        Player.PlayStation(s);
                    }
                    else
                    {
                        ShowStationList();
                    }
                }
                else
                {
                    this.BeginDispatch(ShowStationList);
                }
            }
            else
            {
                transitionControl.ShowPage(_loginPage);
            }
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Threading.Tasks.Task.Factory.StartNew(LoadLogic);
        }

        private void transitionControl_CurrentPageSet(System.Windows.Controls.UserControl page)
        {
            if (Equals(page, _loadingPage) && _initComplete && !_finalComplete)
                System.Threading.Tasks.Task.Factory.StartNew(FinalLoad);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_forceClose && _config.Fields.Elpis_MinimizeToTray && !_restarting)
            {
                WindowState = System.Windows.WindowState.Minimized;
                Hide();
                ShowInTaskbar = false;

                e.Cancel = true;
                return;
            }

            if (_notify != null)
            {
                _notify.Dispose();
                _notify = null;
            }

            if (_config != null)
            {
                _config.Fields.Elpis_StartupLocation = new System.Windows.Point(Left, Top);
                _config.Fields.Elpis_StartupSize = new System.Windows.Size(Width, Height);
                if (Player != null)
                    _config.Fields.Elpis_Volume = Player.Volume;
                _config.SaveConfig();
            }
            StopWebServer();
        }

        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized && _config.Fields.Elpis_MinimizeToTray)
            {
                Hide();
                ShowInTaskbar = false;
            }
            else
            {
                Show();
                ShowInTaskbar = true;
            }
        }

        public void LoadStation(string station)
        {
            PandoraSharp.Station s = Player.GetStationFromString(station);
            if (s != null)
            {
                Player.PlayStation(s);
            }
        }

        public void PlayPauseToggled(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            //this is inverse because of being applied before action is taken
            ShowBalloon(!Player.Paused ? PAUSE : PLAY);
            Player.PlayPause();
        }

        public void SkipTrack(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ShowBalloon(SKIP);
            Player.Next();
        }

        private void ShowBalloon(int option, int duration = 3000)
        {
            if (!_config.Fields.Elpis_ShowTrayNotifications) return;
            if (WindowState != System.Windows.WindowState.Minimized) return;
            switch (option)
            {
                case PLAY:
                {
                    string tipText = Player.CurrentSong.SongTitle;
                    _notify.BalloonTipTitle = @"Playing: " + tipText;
                    _notify.BalloonTipText = @" by " + Player.CurrentSong.Artist;
                    break;
                }
                case PAUSE:
                {
                    _notify.BalloonTipTitle = @"Paused";
                    _notify.BalloonTipText = @" ";
                    break;
                }
                case LIKE:
                {
                    //this is inverse because of being applied before action is taken
                    _notify.BalloonTipTitle = !GetCurrentSong().Loved ? "Song Liked" : "Song Unliked";
                    _notify.BalloonTipText = @" ";
                    break;
                }
                case DISLIKE:
                {
                    _notify.BalloonTipTitle = @"Song Disliked";
                    _notify.BalloonTipText = @" ";
                    break;
                }
                case SKIP:
                {
                    _notify.BalloonTipTitle = @"Song Skipped";
                    _notify.BalloonTipText = @" ";
                    break;
                }
                default:
                {
                    return;
                }
            }
            _notify.ShowBalloonTip(3000);
        }

        private void CanExecutePlayPauseSkip(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !_isActiveWindow || IsOnPlaylist();
        }

        public void ExecuteThumbsUp(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ShowBalloon(LIKE);
            PlaylistPage.ThumbUpCurrent();
        }

        public void ExecuteThumbsDown(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ShowBalloon(DISLIKE);
            PlaylistPage.ThumbDownCurrent();
        }

        private void CanExecuteThumbsUpDown(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            if (!_isActiveWindow && Player.CurrentSong != null)
            {
                e.CanExecute = true;
            }
            else
            {
                if (IsOnPlaylist() && Player.CurrentSong != null)
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
        }

        #endregion
    }
}