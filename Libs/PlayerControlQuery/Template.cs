using Elpis.PandoraSharp;

namespace Elpis.PlayerControlQuery
{
    //Use this as a template for classes that inherit IPlayerControlQuery
    public class Template : IPlayerControlQuery
    {
        public event PlayStateRequestEvent PlayStateRequest;
        public event PlayRequestEvent PlayRequest;
        public event PauseRequestEvent PauseRequest;
        public event NextRequestEvent NextRequest;
        public event StopRequestEvent StopRequest;

        public event SetSongMetaRequestEvent SetSongMetaRequest;

        public void SongUpdateReceiver(QuerySong song) {}

        public void ProgressUpdateReciever(QueryProgress progress) {}

        public void StatusUpdateReceiver(QueryStatus status) {}

        public void RatingUpdateReceiver(QuerySong song, SongRating oldRating,
            SongRating newRating) {}
    }
}