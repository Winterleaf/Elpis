namespace Lpfm.LastFmScrobbler.Api
{
    /// <summary>
    ///     A Last.fm API exception
    /// </summary>
    public class LastFmApiException : System.Exception
    {
        /// <summary>
        ///     Instantiates a Last.fm API exception
        /// </summary>
        public LastFmApiException(string message) : base(message) {}

        /// <summary>
        ///     Instantiates a Last.fm API exception
        /// </summary>
        public LastFmApiException(string message, System.Exception innerException) : base(message, innerException) {}
    }
}