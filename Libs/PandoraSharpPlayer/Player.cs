/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of PandoraSharpPlayer.
 * PandoraSharpPlayer is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * PandoraSharpPlayer is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with PandoraSharpPlayer. If not, see http://www.gnu.org/licenses/.
*/

using System.IO;
using System.Linq;
using Elpis.BassPlayer;
using Elpis.PandoraSharp;

namespace Elpis.PandoraSharpPlayer
{
    public class Player : System.ComponentModel.INotifyPropertyChanged
    {
        public string OutputDevice
        {
            get { return _bass.SoundDevice; }
            set { _bass.SoundDevice = value; }
        }

        public bool PauseOnLock
        {
            get { return _sessionWatcher.IsEnabled; }
            set { _sessionWatcher.IsEnabled = value; }
        }

        public int MaxPlayed
        {
            get { return _playlist.MaxPlayed; }
            set { _playlist.MaxPlayed = value; }
        }

        private BassAudioEngine _bass;

        private PlayerControlQuery.ControlQueryManager _cqman;
        private Pandora _pandora;
        private Playlist _playlist;

        private bool _playNext;

        private SessionWatcher _sessionWatcher;

        public bool Initialize(string bassRegEmail = "", string bassRegKey = "")
        {
            _cqman = new PlayerControlQuery.ControlQueryManager();
            _cqman.NextRequest += _cqman_NextRequest;
            _cqman.PauseRequest += _cqman_PauseRequest;
            _cqman.PlayRequest += _cqman_PlayRequest;
            _cqman.StopRequest += _cqman_StopRequest;
            _cqman.PlayStateRequest += _cqman_PlayStateRequest;
            _cqman.SetSongMetaRequest += _cqman_SetSongMetaRequest;

            _sessionWatcher = new SessionWatcher();
            RegisterPlayerControlQuery(_sessionWatcher);

            _pandora = new Pandora();
            _pandora.ConnectionEvent += _pandora_ConnectionEvent;
            _pandora.StationUpdateEvent += _pandora_StationUpdateEvent;
            _pandora.FeedbackUpdateEvent += _pandora_FeedbackUpdateEvent;
            _pandora.LoginStatusEvent += _pandora_LoginStatusEvent;
            _pandora.StationsUpdatingEvent += _pandora_StationsUpdatingEvent;
            _pandora.QuickMixSavedEvent += _pandora_QuickMixSavedEvent;

            _bass = new BassAudioEngine(bassRegEmail, bassRegKey);
            _bass.PlaybackProgress += bass_PlaybackProgress;
            _bass.PlaybackStateChanged += bass_PlaybackStateChanged;
            _bass.PlaybackStart += bass_PlaybackStart;
            _bass.PlaybackStop += bass_PlaybackStop;
            _bass.InitBass();

            _playlist = new Playlist {MaxPlayed = 8};
            _playlist.PlaylistLow += _playlist_PlaylistLow;
            _playlist.PlayedSongQueued += _playlist_PlayedSongQueued;
            _playlist.PlayedSongDequeued += _playlist_PlayedSongDequeued;

            DailySkipLimitReached = false;
            DailySkipLimitTime = System.DateTime.MinValue;

            LoggedIn = false;
            return true;
        }

        public System.Collections.Generic.IList<string> GetOutputDevices() => _bass.GetOutputDevices().Where(x => x != "No sound").ToList();

        private void _cqman_SetSongMetaRequest(object sender, object meta)
        {
            CurrentSong?.SetMetaObject(sender, meta);
        }

        private PlayerControlQuery.QueryStatusValue _cqman_PlayStateRequest(object sender)
        {
            return _cqman.LastQueryStatus;
        }

        private void _cqman_StopRequest(object sender)
        {
            Stop();
        }

        private void _cqman_PlayRequest(object sender)
        {
            Play();
        }

        private void _cqman_PauseRequest(object sender)
        {
            Pause();
        }

        private void _cqman_NextRequest(object sender)
        {
            Next();
        }

        public void RegisterPlayerControlQuery(PlayerControlQuery.IPlayerControlQuery obj)
        {
            _cqman?.RegisterPlayerControlQuery(obj);
        }

        public void SetProxy(string address, int port, string user = "", string password = "")
        {
            Util.PRequest.SetProxy(address, port, user, password);
            _bass?.SetProxy(address, port, user, password);
        }

        #region Playlist Event Handlers

        private void _playlist_PlaylistLow(object sender, int count)
        {
            RunTask(() => UpdatePlaylist());
        }

        #endregion

        #region Recording Code

        private static string CleanFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return new string (fileName.Where(x => !invalidChars.Contains(x)).ToArray());
            
        }

        private static string CleanDirectoryName(string directoryName)
        {
            var invalidChars = Path.GetInvalidPathChars().ToList();
            invalidChars.Add('/');
            invalidChars.Add('\\');
            return new string(directoryName.Where(x => !invalidChars.Contains(x)).ToArray());
            
        }
        
        private static Song LastSong { get; set; }

        public bool Rip { get; set; }
        public string RipPath { get; set; }

        #endregion

        #region Events

        #region Delegates

        public delegate void ConnectionEventHandler(object sender, bool state, Util.ErrorCodes code);

        public delegate void LogoutEventHandler(object sender);

        public delegate void ExceptionEventHandler(object sender, Util.ErrorCodes code, System.Exception ex);

        public delegate void FeedbackUpdateEventHandler(object sender, Song song, bool success);

        public delegate void LoadingNextSongHandler(object sender);

        public delegate void LoginStatusEventHandler(object sender, string status);

        public delegate void PlaybackProgressHandler(object sender, BassAudioEngine.Progress prog);

        public delegate void PlaybackStartHandler(object sender, double duration);

        public delegate void PlaybackStateChangedHandler(object sender, BassAudioEngine.PlayState oldState, BassAudioEngine.PlayState newState);

        public delegate void PlaybackStopHandler(object sender);

        public delegate void PlaylistSongHandler(object sender, Song song);

        public delegate void SearchResultHandler(object sender, System.Collections.Generic.List<SearchResult> result);

        public delegate void StationCreatedHandler(object sender, Station station);

        public delegate void StationLoadedHandler(object sender, Station station);

        public delegate void StationLoadingHandler(object sender, Station station);

        public delegate void StationsRefreshedHandler(object sender);

        public delegate void StationsRefreshingHandler(object sender);

        public delegate void QuickMixSavedEventHandler(object sender);

        #endregion

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public event PlaybackProgressHandler PlaybackProgress;
        public event PlaybackStateChangedHandler PlaybackStateChanged;
        public event PlaybackStartHandler PlaybackStart;
        public event PlaybackStopHandler PlaybackStop;

        public event PlaylistSongHandler SongStarted;
        public event PlaylistSongHandler PlayedSongAdded;
        public event PlaylistSongHandler PlayedSongRemoved;

        public event StationLoadingHandler StationLoading;

        public event StationLoadedHandler StationLoaded;

        public event ConnectionEventHandler ConnectionEvent;

        public event LogoutEventHandler LogoutEvent;

        public event FeedbackUpdateEventHandler FeedbackUpdateEvent;

        public event ExceptionEventHandler ExceptionEvent;

        public event StationsRefreshedHandler StationsRefreshed;

        public event StationsRefreshingHandler StationsRefreshing;

        public event LoadingNextSongHandler LoadingNextSong;

        public event SearchResultHandler SearchResult;

        public event StationCreatedHandler StationCreated;

        public event QuickMixSavedEventHandler QuickMixSavedEvent;

        public event LoginStatusEventHandler LoginStatusEvent;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(info));
        }

        #endregion

        #region Properties

        private bool _paused;
        private bool _playing;
        private bool _stopped = true;
        public string Email { get; set; }
        public string Password { get; set; }

        public System.Collections.Generic.List<Station> Stations => _pandora.Stations;

        public string ImageCachePath
        {
            get
            {
                if (_pandora != null)
                {
                    return _pandora.ImageCachePath;
                }
                throw new System.Exception("Pandora object has not been initialized, cannot get ImagePathCache");
            }
            set
            {
                if (_pandora != null)
                {
                    if (!Directory.Exists(value))
                        throw new System.Exception("ImagePathCache directory does not exist!");

                    _pandora.ImageCachePath = value;
                }
                else
                {
                    throw new System.Exception("Pandora object has not been initialized, cannot get ImagePathCache");
                }
            }
        }

        public bool LoggedIn { get; set; }

        [System.ComponentModel.DefaultValue(null)]
        public Station CurrentStation { get; private set; }

        public bool IsStationLoaded => CurrentStation != null;

        public Song CurrentSong => _playlist.Current;

        public string AudioFormat
        {
            get
            {
                return _pandora != null ? _pandora.AudioFormat : "";
            }

            set
            {
                _pandora?.SetAudioFormat(value);
            }
        }

        public bool ForceSsl
        {
            get
            {
                return _pandora != null && _pandora.ForceSsl;
            }

            set
            {
                if (_pandora != null)
                {
                    _pandora.ForceSsl = value;
                }
            }
        }

        public Pandora.SortOrder StationSortOrder
        {
            get
            {
                return _pandora?.StationSortOrder ?? Pandora.SortOrder.DateDesc;
            }

            set
            {
                if (_pandora != null)
                {
                    SetStationSortOrder(value);
                }
            }
        }

        public bool Paused
        {
            get { return _paused; }

            private set
            {
                if (value == _paused) return;
                _paused = value;
                NotifyPropertyChanged("Paused");
            }
        }

        public bool Playing
        {
            get { return _playing; }

            private set
            {
                if (value == _playing) return;
                _playing = value;
                NotifyPropertyChanged("Playing");
            }
        }

        public bool Stopped
        {
            get { return _stopped; }

            private set
            {
                if (value == _stopped) return;
                _stopped = value;
                NotifyPropertyChanged("Stopped");
            }
        }

        public int Volume
        {
            get { return _bass.Volume; }
            set { _bass.Volume = value; }
        }

        public bool DailySkipLimitReached { get; set; }
        public System.DateTime DailySkipLimitTime { get; set; }

        #endregion

        #region Private Methods

        private void SendPandoraError(Util.ErrorCodes code, System.Exception ex)
        {
            ExceptionEvent?.Invoke(this, code, ex);

            _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.Error);
        }

        private void PlayNextSong(int retry = 2)
        {
            if (_playNext && retry >= 2) return;
            _playNext = true;
            Song song;
            if (LoadingNextSong != null)
            {
                Util.Log.O("Loading next song.");
                LoadingNextSong(this);
            }

            try
            {
                song = _playlist.NextSong();
            }
            catch (PandoraException pex)
            {
                _playNext = false;
                if (pex.Fault != Util.ErrorCodes.EndOfPlaylist) throw;
                Stop();
                return;
            }

            Util.Log.O("Play: " + song);

            SongStarted?.Invoke(this, song);

            if (Rip && LastSong != null)
            {
                try
                {
                    long length = new FileInfo(LastSong.FileName).Length;
                    System.Console.WriteLine("File Length: " + length);
                    //392880
                    //Lets delete any commercials.
                    if (length < 500000)
                        File.Delete(LastSong.FileName);
                    else
                    {
                        try
                        {
                            using (TagLib.File file = TagLib.File.Create(LastSong.FileName))
                            {
                                file.Tag.Album = LastSong.Album;
                                file.Tag.Title = LastSong.SongTitle;
                                file.Tag.AlbumArtists = new[] { LastSong.Artist };
                                file.Tag.AmazonId = LastSong.AmazonTrackId;
                                file.Save();
                            }
                        }
                        catch (System.Exception)
                        {
                            //ignore
                            
                        }
                        
                    }
                }
                catch (System.Exception err)
                {
                    Util.Log.O(err.Message + err.StackTrace);
                }
            }

            try
            {
                if (Rip)
                {
                    try
                    {
                        string filenamex = Path.Combine(Path.Combine(RipPath,CleanDirectoryName( song.Artist)),CleanDirectoryName( song.Album));

                        if (!Directory.Exists(filenamex))
                            Directory.CreateDirectory(filenamex);

                        string filename = Path.Combine(filenamex, CleanFileName( song.SongTitle + ".mp3"));

                        if (File.Exists(filename))
                        {
                            LastSong = null;
                            _bass.Play(song.AudioUrl, song.FileGain);
                        }

                        LastSong = song;
                        LastSong.FileName = filename;
                        _bass.PlayStreamWithDownload(song.AudioUrl, filename, song.FileGain);
                    }
                    catch (System.Exception err)
                    {
                        Util.Log.O(err.Message + err.StackTrace);
                        throw;
                    }
                }
                else
                {
                    LastSong = null;
                    _bass.Play(song.AudioUrl, song.FileGain);
                }

                _cqman.SendSongUpdate(song);
                //_cqman.SendStatusUpdate(QueryStatusValue.Playing);
            }
            catch (BassStreamException ex)
            {
                if (ex.ErrorCode == Un4seen.Bass.BASSError.BASS_ERROR_FILEOPEN)
                {
                    _playlist.DoReload();
                }
                if (retry > 0)
                    PlayNextSong(retry - 1);
                else
                {
                    Stop();
                    _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.Error);
                    throw new PandoraException(Util.ErrorCodes.StreamError, ex);
                }
            }
            finally
            {
                _playNext = false;
            }

            _playNext = false;
        }

        private int UpdatePlaylist()
        {
            System.Collections.Generic.List<Song> result = new System.Collections.Generic.List<Song>();
            try
            {
                result = CurrentStation.GetPlaylist();
            }
            catch (PandoraException ex)
            {
                if (ex.Message == "DAILY_SKIP_LIMIT_REACHED")
                {
                    DailySkipLimitReached = true;
                    DailySkipLimitTime = System.DateTime.Now;
                }
            }

            if (result.Count == 0 && CurrentStation != null)
                result = CurrentStation.GetPlaylist();

            return _playlist.AddSongs(result);
        }

        private void PlayThread()
        {
            StationLoading?.Invoke(this, CurrentStation);
            _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.StationLoading);

            _playlist.ClearSongs();

            if (UpdatePlaylist() == 0)
                throw new PandoraException(Util.ErrorCodes.EndOfPlaylist);

            StationLoaded?.Invoke(this, CurrentStation);
            _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.StationLoaded);

            try
            {
                PlayNextSong();
            }
            catch (Playlist.PlaylistEmptyException)
            {
                if (UpdatePlaylist() == 0)
                    throw new PandoraException(Util.ErrorCodes.EndOfPlaylist);

                PlayNextSong();
            }
        }

        #endregion

        #region Public Methods

        private void RunTask(System.Action method)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    method();
                }
                catch (PandoraException pex)
                {
                    Util.Log.O(pex.Fault + ": " + pex);
                    SendPandoraError(pex.Fault, pex);
                }
                catch (System.Exception ex)
                {
                    Util.Log.O(ex.ToString());

                    SendPandoraError(Util.ErrorCodes.UnknownError, ex);
                }
            });
        }

        public void Logout()
        {
            LoggedIn = false;
            Stop();
            CurrentStation = null;
            _playlist.ClearSongs();
            _playlist.ClearHistory();
            _playlist.Current = null;
            _pandora.Logout();
            Email = string.Empty;
            Password = string.Empty;

            LogoutEvent?.Invoke(this);
            _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.Disconnected);
        }

        public void Connect(string email, string password)
        {
            LoggedIn = false;
            Email = email;
            Password = password;
            RunTask(() => _pandora.Connect(Email, Password));
            _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.Connecting);
        }

        public Station GetStationFromString(string nameOrId)
        {
            if (nameOrId == null) return null;
            Station s = System.Text.RegularExpressions.Regex.IsMatch(nameOrId, @"^[0-9]+$") ? GetStationFromId(nameOrId) : GetStationFromName(nameOrId);

            return s;
        }

        public Station GetStationFromId(string stationId)
        {
            return Stations.FirstOrDefault(s => stationId == s.Id);
        }

        public Station GetStationFromName(string stationName)
        {
            return Stations.FirstOrDefault(s => stationName == s.Name);
        }

        public void PlayStation(Station station)
        {
            CurrentStation = station;
            //JumpList.AddToRecentCategory(station.asJumpTask());

            RunTask(PlayThread);
        }

        public bool PlayStation(string stationId)
        {
            Station s = GetStationFromId(stationId);
            if (s == null) return false;
            PlayStation(s);
            return true;
        }

        private bool _isRating;

        private void RateSong(Song song, SongRating rating)
        {
            if (_isRating) return;
            _isRating = true;
            SongRating oldRating = song.Rating;
            song.Rate(rating);
            _cqman.SendRatingUpdate(song.Artist, song.Album, song.SongTitle, oldRating, rating);
            _isRating = false;
        }

        public void SongThumbUp(Song song)
        {
            if (!IsStationLoaded) return;
            RunTask(() => RateSong(song, SongRating.Love));
        }

        public void SongThumbDown(Song song)
        {
            if (!IsStationLoaded) return;
            RunTask(() => RateSong(song, SongRating.Ban));
        }

        public void SongDeleteFeedback(Song song)
        {
            if (!IsStationLoaded) return;
            RunTask(() => RateSong(song, SongRating.None));
        }

        public void SongTired(Song song)
        {
            if (!IsStationLoaded) return;
            RunTask(song.SetTired);
        }

        public void SongBookmarkArtist(Song song)
        {
            if (!IsStationLoaded) return;
            RunTask(song.BookmarkArtist);
        }

        public void SongBookmark(Song song)
        {
            if (!IsStationLoaded) return;
            RunTask(song.Bookmark);
        }

        public void StationRename(Station station, string name)
        {
            RunTask(() => station.Rename(name));
        }

        public void SetStationSortOrder(Pandora.SortOrder order)
        {
            _pandora.StationSortOrder = order;
        }

        public void SetStationSortOrder(string order)
        {
            Pandora.SortOrder sort;
            System.Enum.TryParse(order, true, out sort);
            SetStationSortOrder(sort);
        }

        public void RefreshStations()
        {
            RunTask(() => _pandora.RefreshStations());
        }

        public void StationDelete(Station station)
        {
            RunTask(() =>
            {
                bool playQuickMix = CurrentStation != null && station.Id == CurrentStation.Id;
                station.Delete();
                _pandora.RefreshStations();
                if (!playQuickMix) return;
                Util.Log.O("Current station deleted, playing Quick Mix");
                PlayStation(Stations[0]); //Set back to quickmix because current was deleted
            });
        }

        public void StationSearchNew(string query)
        {
            RunTask(() =>
            {
                System.Collections.Generic.List<SearchResult> result = _pandora.Search(query);
                SearchResult?.Invoke(this, result);
            });
        }

        public void CreateStationFromSong(Song song)
        {
            RunTask(() =>
            {
                Station station = _pandora.CreateStationFromSong(song);
                StationCreated?.Invoke(this, station);
            });
        }

        public void CreateStationFromArtist(Song song)
        {
            RunTask(() =>
            {
                Station station = _pandora.CreateStationFromArtist(song);
                StationCreated?.Invoke(this, station);
            });
        }

        public void CreateStation(SearchResult result)
        {
            RunTask(() =>
            {
                Station station = _pandora.CreateStationFromSearch(result.MusicToken);
                StationCreated?.Invoke(this, station);
            });
        }

        public void SaveQuickMix()
        {
            RunTask(() => { _pandora.SaveQuickMix(); });
        }

        private bool _isPlayPause;

        public void PlayPause()
        {
            if (!IsStationLoaded) return;
            RunTask(() =>
            {
                if (_isPlayPause) return;
                _isPlayPause = true;
                _bass.PlayPause();
                _isPlayPause = false;
            });
        }

        public void Play()
        {
            if (!IsStationLoaded) return;
            if (Paused) PlayPause();
        }

        public void Pause()
        {
            if (!IsStationLoaded) return;
            if (Playing) PlayPause();
        }

        public void Stop()
        {
            if (!IsStationLoaded) return;
            RunTask(() =>
            {
                CurrentStation = null;
                _bass.Stop();
            });
        }

        private bool _isNext;

        public void Next()
        {
            if (!IsStationLoaded) return;
            RunTask(() =>
            {
                if (_isNext) return;
                _isNext = true;
                PlayNextSong();
                _isNext = false;
            });
        }

        #endregion

        #region Pandora Handlers

        private void _pandora_StationsUpdatingEvent(object sender)
        {
            StationsRefreshing?.Invoke(this);
        }

        private void _pandora_QuickMixSavedEvent(object sender)
        {
            QuickMixSavedEvent?.Invoke(this);
        }

        private void _pandora_LoginStatusEvent(object sender, string status)
        {
            LoginStatusEvent?.Invoke(this, status);
        }

        private void _pandora_FeedbackUpdateEvent(object sender, Song song, bool success)
        {
            FeedbackUpdateEvent?.Invoke(this, song, success);
        }

        private void _pandora_StationUpdateEvent(object sender)
        {
            StationsRefreshed?.Invoke(this);
        }

        private void _playlist_PlayedSongDequeued(object sender, Song oldSong)
        {
            PlayedSongRemoved?.Invoke(this, oldSong);
        }

        private void _playlist_PlayedSongQueued(object sender, Song newSong)
        {
            PlayedSongAdded?.Invoke(this, newSong);
        }

        private void _pandora_ConnectionEvent(object sender, bool state, Util.ErrorCodes code)
        {
            LoggedIn = state;

            ConnectionEvent?.Invoke(this, state, code);

            _cqman.SendStatusUpdate(state ? PlayerControlQuery.QueryStatusValue.Connected : PlayerControlQuery.QueryStatusValue.Error);
        }

        #endregion

        #region BassPlayer Event Handlers

        private void bass_PlaybackProgress(object sender, BassAudioEngine.Progress prog)
        {
            PlaybackProgress?.Invoke(this, prog);
            _cqman.SendProgressUpdate(_cqman.LastSong, prog.TotalTime, prog.ElapsedTime);
        }

        private void bass_PlaybackStateChanged(object sender, BassAudioEngine.PlayState oldState, BassAudioEngine.PlayState newState)
        {
            PlaybackStateChanged?.Invoke(this, oldState, newState);

            Util.Log.O("Playstate: " + newState);

            Paused = newState == BassAudioEngine.PlayState.Paused;
            Playing = newState == BassAudioEngine.PlayState.Playing;
            Stopped = newState == BassAudioEngine.PlayState.Ended || newState == BassAudioEngine.PlayState.Stopped;

            if (Playing) _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.Playing);
            else if (Paused) _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.Paused);
            else if (Stopped) _cqman.SendStatusUpdate(PlayerControlQuery.QueryStatusValue.Stopped);

            if (newState != BassAudioEngine.PlayState.Ended || CurrentStation == null) return;
            Util.Log.O("Song ended, playing next song.");
            RunTask(() => PlayNextSong());
        }

        private void bass_PlaybackStart(object sender, double duration)
        {
            if (CurrentSong != null)
                CurrentSong.Played = true;

            PlaybackStart?.Invoke(this, duration);
        }

        private void bass_PlaybackStop(object sender)
        {
            PlaybackStop?.Invoke(this);
        }

        #endregion
    }
}