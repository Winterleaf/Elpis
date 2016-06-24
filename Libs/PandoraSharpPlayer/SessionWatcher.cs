using Elpis.PandoraSharp;

namespace Elpis.PandoraSharpPlayer
{
    public class SessionWatcher : PlayerControlQuery.IPlayerControlQuery
    {
        public SessionWatcher()
        {
            Util.SystemSessionState sessionState = new Util.SystemSessionState();
            sessionState.SystemLocked += _sessionState_SystemLocked;
            sessionState.SystemUnlocked += _sessionState_SystemUnlocked;
        }

        public bool IsEnabled { get; set; }

        private PlayerControlQuery.QueryStatusValue _oldState =
            PlayerControlQuery.QueryStatusValue.Invalid;

        public event PlayerControlQuery.PlayStateRequestEvent PlayStateRequest;
        public event PlayerControlQuery.PlayRequestEvent PlayRequest;
        public event PlayerControlQuery.PauseRequestEvent PauseRequest;
        public event PlayerControlQuery.NextRequestEvent NextRequest;
        public event PlayerControlQuery.StopRequestEvent StopRequest;

        public event PlayerControlQuery.SetSongMetaRequestEvent SetSongMetaRequest;

        public void SongUpdateReceiver(PlayerControlQuery.QuerySong song) {}

        public void ProgressUpdateReciever(PlayerControlQuery.QueryProgress progress) {}

        public void StatusUpdateReceiver(PlayerControlQuery.QueryStatus status) {}

        public void RatingUpdateReceiver(PlayerControlQuery.QuerySong song, SongRating oldRating,
            SongRating newRating) {}

        private void _sessionState_SystemUnlocked()
        {
            if (!IsEnabled) return;

            if (_oldState == PlayerControlQuery.QueryStatusValue.Playing)
                PlayRequest?.Invoke(this);
        }

        private void _sessionState_SystemLocked()
        {
            if (!IsEnabled) return;

            System.Diagnostics.Debug.Assert(PlayStateRequest != null, "PlayStateRequest != null");
            _oldState = PlayStateRequest(this);
            if (_oldState != PlayerControlQuery.QueryStatusValue.Playing) return;
            System.Diagnostics.Debug.Assert(PauseRequest != null, "PauseRequest != null");
            PauseRequest(this);
        }
    }
}