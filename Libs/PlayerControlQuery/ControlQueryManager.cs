using Elpis.PandoraSharp;

namespace Elpis.PlayerControlQuery
{
    public class ControlQueryManager
    {
        public ControlQueryManager()
        {
            _lastQuerySong = null;

            _pcqList = new System.Collections.Generic.List<IPlayerControlQuery>();
        }

        public Song LastSong
        {
            get
            {
                lock (_lastQuerySongLock)
                {
                    return _lastQuerySong;
                }
            }
        }

        public QueryStatusValue LastQueryStatus
        {
            get
            {
                lock (_lastQueryStatusLock)
                {
                    return _lastQueryStatus;
                }
            }
        }

        private Song _lastQuerySong;

        private readonly object _lastQuerySongLock = new object();
        private QueryStatusValue _lastQueryStatus = QueryStatusValue.Waiting;

        private readonly object _lastQueryStatusLock = new object();
        private readonly System.Collections.Generic.List<IPlayerControlQuery> _pcqList;

        public void RegisterPlayerControlQuery(IPlayerControlQuery obj)
        {
            _pcqList.Add(obj);

            obj.PlayStateRequest += PlayStateRequestHandler;
            obj.PlayRequest += PlayRequestHandler;
            obj.PauseRequest += PauseRequestHandler;
            obj.NextRequest += NextRequestHandler;
            obj.StopRequest += StopRequestHandler;

            obj.SetSongMetaRequest += obj_SetSongMetaRequest;
        }

        public event PlayStateRequestEvent PlayStateRequest;
        public event PlayRequestEvent PlayRequest;
        public event PauseRequestEvent PauseRequest;
        public event NextRequestEvent NextRequest;
        public event StopRequestEvent StopRequest;

        public event SetSongMetaRequestEvent SetSongMetaRequest;

        private void obj_SetSongMetaRequest(object sender, object meta)
        {
            SetSongMetaRequest?.Invoke(sender, meta);
        }

        private void StopRequestHandler(object sender)
        {
            StopRequest?.Invoke(sender);
        }

        private void NextRequestHandler(object sender)
        {
            NextRequest?.Invoke(sender);
        }

        private void PauseRequestHandler(object sender)
        {
            PauseRequest?.Invoke(sender);
        }

        private void PlayRequestHandler(object sender)
        {
            PlayRequest?.Invoke(sender);
        }

        private QueryStatusValue PlayStateRequestHandler(object sender)
        {
            if (PlayStateRequest != null) return PlayStateRequest(sender);
            return QueryStatusValue.Invalid;
        }

        public void SendSongUpdate(Song song)
        {
            lock (_lastQuerySongLock)
            {
                _lastQuerySong = song;
            }

            Util.Log.O("Song Update: {0} | {1} | {2}", song.Artist, song.Album, song.SongTitle);
            foreach (IPlayerControlQuery obj in _pcqList)
            {
                obj.SongUpdateReceiver(new QuerySong
                {
                    Artist = song.Artist,
                    Album = song.Album,
                    Title = song.SongTitle,
                    Meta = song.GetMetaObject(obj)
                });
            }
        }

        public void SendStatusUpdate(QueryStatus status)
        {
            lock (_lastQueryStatusLock)
            {
                _lastQueryStatus = status.CurrentStatus;
            }

            Util.Log.O("Status Update: {0} -> {1}", status.PreviousStatus.ToString(), status.CurrentStatus.ToString());

            foreach (IPlayerControlQuery obj in _pcqList)
            {
                obj.StatusUpdateReceiver(status);
            }
        }

        public void SendStatusUpdate(QueryStatusValue previous, QueryStatusValue current)
        {
            SendStatusUpdate(new QueryStatus {PreviousStatus = previous, CurrentStatus = current});
        }

        public void SendStatusUpdate(QueryStatusValue current)
        {
            SendStatusUpdate(_lastQueryStatus, current);
        }

        public void SendProgressUpdate(Song song, QueryTrackProgress progress)
        {
            foreach (IPlayerControlQuery obj in _pcqList)
            {
                QueryProgress prog = new QueryProgress
                {
                    Song =
                        new QuerySong
                        {
                            Artist = song.Artist,
                            Album = song.Album,
                            Title = song.SongTitle,
                            Meta = song.GetMetaObject(obj)
                        },
                    Progress = progress
                };

                obj.ProgressUpdateReciever(prog);
            }
        }

        public void SendProgressUpdate(Song song, System.TimeSpan totalTime, System.TimeSpan elapsedTime)
        {
            SendProgressUpdate(song, new QueryTrackProgress {TotalTime = totalTime, ElapsedTime = elapsedTime});
        }

        public void SendRatingUpdate(QuerySong song, SongRating oldRating, SongRating newRating)
        {
            foreach (IPlayerControlQuery obj in _pcqList)
            {
                obj.RatingUpdateReceiver(song, oldRating, newRating);
            }
        }

        public void SendRatingUpdate(string artist, string album, string song, SongRating oldRating,
            SongRating newRating)
        {
            SendRatingUpdate(new QuerySong {Artist = artist, Album = album, Title = song}, oldRating, newRating);
        }
    }
}