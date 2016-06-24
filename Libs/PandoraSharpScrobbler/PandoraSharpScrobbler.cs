namespace PandoraSharpScrobbler
{
    public class PandoraSharpScrobbler : PlayerControlQuery.IPlayerControlQuery
        /* This class maintains a Last.fm ScrobbleManager object, monitors Elpis's
     * play progress and adds tracks to the scrobbling queue as they satisfy 
     * the 'played' definition - currently hardcoded to half the tracklength */
    {
        public PandoraSharpScrobbler(string lastFmApiKey, string lastFmApiSecret, string lastFmSessionKey = null)
        {
            ApiKey = lastFmApiKey;
            ApiSecret = lastFmApiSecret;
            SessionKey = lastFmSessionKey;
            AuthUrl = string.Empty;

            InitScrobblers();

            _processScrobbleDelegate = DoScrobbles;
        }

        private const double PercentNowPlaying = 5.0;
        private const double SecondsBeforeScrobble = 4*60 + 5;
        private const double PercentBeforeScrobble = 51.0;
        private const double MinTrackLength = 35.0; //buffered to 35 seconds, just in case 

        public bool IsEnabled
        {
            get { return _isEnabled && ApiKey != "dummy_key"; }
            set { _isEnabled = value; }
        }

        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }

        public string SessionKey
        {
            get { return _sessionKey; }
            set
            {
                _sessionKey = value;
                InitScrobblers();
            }
        }

        public string AuthUrl { get; set; }
        private bool _doneNowPlaying;

        private bool _doneScrobble;

        private bool _isEnabled;

        private readonly ProcessScrobblesDelegate _processScrobbleDelegate;

        private Lpfm.LastFmScrobbler.QueuingScrobbler _scrobbler;
        private string _sessionKey = string.Empty;

        private void InitScrobblers()
        {
            _scrobbler = new Lpfm.LastFmScrobbler.QueuingScrobbler(ApiKey, ApiSecret, SessionKey);
        }

        private void DoScrobbles()
        {
            try
            {
                System.Collections.Generic.List<Lpfm.LastFmScrobbler.Response> response = _scrobbler.Process();
                if (response.Count != 1) return;
                if (response[0].Exception != null)
                {
                    Util.Log.O("Last.FM Error!: " + response[0].Exception);
                }
                if (!(response[0] is Lpfm.LastFmScrobbler.NowPlayingResponse) && !(response[0] is Lpfm.LastFmScrobbler.ScrobbleResponse)) return;
                System.Diagnostics.Debug.Assert(SetSongMetaRequest != null, "SetSongMetaRequest != null");
                SetSongMetaRequest(this, response[0].Track);
            }
            catch (System.Exception ex)
            {
                Util.Log.O("Last.FM Error!: " + ex);
            }
        }

        private void ProcessScrobbles()
        {
            _processScrobbleDelegate.BeginInvoke(null, null);
        }

        public string GetAuthUrl()
        {
            AuthUrl = _scrobbler.BaseScrobbler.GetAuthorisationUri();
            return AuthUrl;
        }

        public void LaunchAuthPage()
        {
            if (AuthUrl == string.Empty)
                AuthUrl = GetAuthUrl();

            System.Diagnostics.Process.Start(AuthUrl);
        }

        public string GetAuthSessionKey()
        {
            try
            {
                SessionKey = _scrobbler.BaseScrobbler.GetSession();
            }
            catch (Lpfm.LastFmScrobbler.Api.LastFmApiException exception)
            {
                Util.Log.O("LastFM Error: " + exception);
                throw;
            }
            return SessionKey;
        }

        private Lpfm.LastFmScrobbler.Track QueryProgressToTrack(PlayerControlQuery.QueryProgress prog)
        {
            Lpfm.LastFmScrobbler.Track track = new Lpfm.LastFmScrobbler.Track
            {
                TrackName = prog.Song.Title,
                AlbumName = prog.Song.Album,
                ArtistName = prog.Song.Artist,
                Duration = prog.Progress.TotalTime,
                WhenStartedPlaying = System.DateTime.Now.Subtract(prog.Progress.ElapsedTime)
            };
            return track;
        }

        private Lpfm.LastFmScrobbler.Track QuerySongToTrack(PlayerControlQuery.QuerySong song)
        {
            Lpfm.LastFmScrobbler.Track track = new Lpfm.LastFmScrobbler.Track
            {
                TrackName = song.Title,
                AlbumName = song.Album,
                ArtistName = song.Artist
            };
            return track;
        }

        public void SetProxy(string address, int port, string user = "", string password = "")
        {
            System.Net.WebProxy p = new System.Net.WebProxy(address, port);

            if (user != "")
                p.Credentials = new System.Net.NetworkCredential(user, password);

            Lpfm.LastFmScrobbler.Scrobbler.SetWebProxy(p);
        }

        private delegate void ProcessScrobblesDelegate();

        #region IPlayerControlQuery Members

        public event PlayerControlQuery.PlayStateRequestEvent PlayStateRequest;
        public event PlayerControlQuery.PlayRequestEvent PlayRequest;
        public event PlayerControlQuery.PauseRequestEvent PauseRequest;
        public event PlayerControlQuery.NextRequestEvent NextRequest;
        public event PlayerControlQuery.StopRequestEvent StopRequest;

        public event PlayerControlQuery.SetSongMetaRequestEvent SetSongMetaRequest;

        public void SongUpdateReceiver(PlayerControlQuery.QuerySong song)
        {
            //Nothing to do here
        }

        public void StatusUpdateReceiver(PlayerControlQuery.QueryStatus status)
        {
            //Nothing to do here
            if (status.CurrentStatus == PlayerControlQuery.QueryStatusValue.Playing &&
                status.PreviousStatus != PlayerControlQuery.QueryStatusValue.Paused)
            {
                _doneNowPlaying = false;
            }
        }

        public void ProgressUpdateReciever(PlayerControlQuery.QueryProgress progress)
        {
            if (!IsEnabled || !_scrobbler.BaseScrobbler.HasSession) return;

            try
            {
                if (progress.Progress.Percent < PercentNowPlaying && !_doneNowPlaying)
                {
                    _doneScrobble = false;
                    Util.Log.O("LastFM, Now Playing: {0} - {1}", progress.Song.Artist, progress.Song.Title);
                    _scrobbler.NowPlaying(QueryProgressToTrack(progress));
                    _doneNowPlaying = true;
                }

                //A track should only be scrobbled when the following conditions have been met: 
                //The track must be longer than 30 seconds. 
                //And the track has been played for at least half its duration, 
                //or for 4 minutes (whichever occurs earlier). 
                //See http://www.last.fm/api/scrobbling
                //This is enforced by LPFM so might as well enforce it here to avoid issues
                if (progress.Progress.TotalTime.TotalSeconds >= MinTrackLength &&
                    (progress.Progress.ElapsedTime.TotalSeconds >= SecondsBeforeScrobble ||
                     progress.Progress.Percent > PercentBeforeScrobble) && !_doneScrobble)
                {
                    _doneNowPlaying = false;
                    Util.Log.O("LastFM, Scrobbling: {0} - {1}", progress.Song.Artist, progress.Song.Title);
                    _scrobbler.Scrobble(QueryProgressToTrack(progress));
                    _doneScrobble = true;
                }

                if (_scrobbler.QueuedCount > 0) ProcessScrobbles();
            }
            catch (System.Exception ex)
            {
                Util.Log.O("Last.FM Error!: " + ex);
            }
        }

        public void RatingUpdateReceiver(PlayerControlQuery.QuerySong song, PandoraSharp.SongRating oldRating, PandoraSharp.SongRating newRating)
        {
            if (!IsEnabled || !_scrobbler.BaseScrobbler.HasSession) return;

            try
            {
                Util.Log.O("LastFM, Rating: {0} - {1} - {2}", song.Artist, song.Title, newRating.ToString());
                Lpfm.LastFmScrobbler.Track track = null;

                //Get corrected track if there is one
                //Without getting the corrected track, 
                //ratings will not work if there were corrections.
                if (song.Meta == null)
                {
                    track = QuerySongToTrack(song);
                }
                else
                    track = (Lpfm.LastFmScrobbler.Track) song.Meta;

                switch (newRating)
                {
                    case PandoraSharp.SongRating.Love:
                        _scrobbler.UnBan(track);
                        _scrobbler.Love(track);
                        break;
                    case PandoraSharp.SongRating.Ban:
                        _scrobbler.UnLove(track);
                        _scrobbler.Ban(track);
                        break;
                    case PandoraSharp.SongRating.None:
                        switch (oldRating) {
                            case PandoraSharp.SongRating.Love:
                                _scrobbler.UnLove(track);
                                break;
                            case PandoraSharp.SongRating.Ban:
                                _scrobbler.UnBan(track);
                                break;
                            case PandoraSharp.SongRating.None:
                                break;
                            default:
                                throw new System.ArgumentOutOfRangeException(nameof(oldRating), oldRating, null);
                        }
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(newRating), newRating, null);
                }

                if (_scrobbler.QueuedCount > 0) ProcessScrobbles();
            }
            catch (System.Exception ex)
            {
                Util.Log.O("Last.FM Error!: " + ex);
            }
        }

        #endregion
    }
}