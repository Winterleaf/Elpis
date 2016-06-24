using Elpis.Lpfm.LastFmScrobbler.Api;

namespace Elpis.Lpfm.LastFmScrobbler
{
    /// <summary>
    ///     Allows a client to "scrobble" tracks to the Last.fm webservice as they are played
    /// </summary>
    public class Scrobbler
    {
        /// <summary>
        /// </summary>
        /// <param name="apiKey">An API Key from Last.fm. See http://www.last.fm/api/account </param>
        /// <param name="apiSecret">An API Secret from Last.fm. See http://www.last.fm/api/account </param>
        /// <param name="sessionKey">An authorized Last.fm Session Key. See <see cref="GetSession" /></param>
        /// <exception cref="System.ArgumentNullException" />
        public Scrobbler(string apiKey, string apiSecret, string sessionKey = null)
        {
            if (string.IsNullOrEmpty(apiKey)) throw new System.ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrEmpty(apiSecret)) throw new System.ArgumentNullException(nameof(apiSecret));

            Authentication = new Authentication {ApiKey = apiKey, ApiSecret = apiSecret};

            if (!string.IsNullOrEmpty(sessionKey)) Authentication.Session = new Session {Key = sessionKey};

            AuthApi = new AuthApi();
            TrackApi = new TrackApi();
        }

        /// <summary>
        ///     The format string pattern for the Last.fm Authorisation page
        /// </summary>
        public const string RequestAuthorisationUriPattern = "http://www.last.fm/api/auth/?api_key={0}&token={1}";

        /// <summary>
        ///     The minimum allowed track length for scrobbling in Seconds
        /// </summary>
        public const int MinimumScrobbleTrackLengthInSeconds = 30;

        /// <summary>
        /// 
        /// </summary>
        public bool HasSession => Authentication.Session != null;

        private Authentication Authentication { get; }
        internal AuthenticationToken AuthenticationToken { get; set; }

        internal IAuthApi AuthApi { get; set; }
        internal ITrackApi TrackApi { get; set; }

        /// <summary>
        ///     Pass in a WebProxy to be used by LPFM
        /// </summary>
        /// <param name="proxy">A configured WebProxy object</param>
        public static void SetWebProxy(System.Net.WebProxy proxy)
        {
            WebRequestRestApi.SetWebProxy(proxy);
        }

        /// <summary>
        ///     Returns a URI that the user should navigate to in their browser to authorise this Application (API Key) to access
        ///     their account
        /// </summary>
        /// <remarks>See http://www.last.fm/api/desktopauth </remarks>
        public string GetAuthorisationUri()
        {
            // Fetch a request token
            GetAuthenticationToken();

            // Format a URI
            return string.Format(RequestAuthorisationUriPattern, Authentication.ApiKey, AuthenticationToken.Value);
        }

        /// <summary>
        ///     Gets an authorised Session Key for the API Key and Secret provided. You must Authorise first, see
        ///     <see cref="GetAuthorisationUri" />.
        ///     See http://www.last.fm/api/desktopauth for the full Authorisation process. Sets the Authentication Session for this
        ///     instance at the same time
        /// </summary>
        /// <returns>The session key returned from Last.fm as string to be cached or stored by the client</returns>
        /// <remarks>
        ///     Session Keys are forever. Once a client has obtained a session key it should be cached or stored by the client, and
        ///     the next time this
        ///     Scrobbler is instantiated, it should be passed in with the constructor arguments
        /// </remarks>
        /// <exception cref="System.InvalidOperationException" />
        public string GetSession()
        {
            if (!string.IsNullOrEmpty(Authentication.Session?.Key))
                throw new System.InvalidOperationException("This Scrobbler already has a Session Key");

            // Get a token
            GetAuthenticationToken();

            // Request a Session
            AuthApi.GetSession(Authentication, AuthenticationToken);

            if (Authentication.Session == null) throw new System.InvalidOperationException();

            return Authentication.Session.Key;
        }

        /// <summary>
        ///     Submits a Track Update Now Playing request to the Last.fm web service
        /// </summary>
        /// <param name="track">A <see cref="Track" /></param>
        /// <returns>A <see cref="NowPlayingResponse" /></returns>
        public NowPlayingResponse NowPlaying(Track track)
        {
            return TrackApi.UpdateNowPlaying(track, Authentication);
        }

        /// <summary>
        ///     Submits a Track Scrobble request to the Last.fm web service
        /// </summary>
        /// <param name="track">A <see cref="Track" /></param>
        /// <returns>A <see cref="ScrobbleResponse" /></returns>
        /// <remarks>
        ///     A track should only be scrobbled when the following conditions have been met: The track must be longer than 30
        ///     seconds.
        ///     And the track has been played for at least half its duration, or for 4 minutes (whichever occurs earlier). See
        ///     http://www.last.fm/api/scrobbling
        /// </remarks>
        /// <exception cref="System.InvalidOperationException" />
        public ScrobbleResponse Scrobble(Track track)
        {
            if (track.Duration.TotalSeconds < MinimumScrobbleTrackLengthInSeconds)
            {
                throw new System.InvalidOperationException($"Duration is too short. Tracks shorter than {MinimumScrobbleTrackLengthInSeconds} seconds in duration must not be scrobbled");
            }

            if (!track.WhenStartedPlaying.HasValue)
                throw new System.ArgumentException("A Track must have a WhenStartedPlaying value when Scrobbling");

            int minimumPlayingTime = (int) track.Duration.TotalSeconds/2;
            if (minimumPlayingTime > 4*60) minimumPlayingTime = 4*60;
            if (track.WhenStartedPlaying > System.DateTime.Now.AddSeconds(-minimumPlayingTime))
            {
                throw new System.InvalidOperationException(
                    "Track has not been playing long enough. A scrobbled track must have been played for at least half its duration, or for 4 minutes (whichever occurs earlier)");
            }

            return TrackApi.Scrobble(track, Authentication);
        }

        /// <summary>
        ///     Submits a Track Love request to the Last.fm web service
        /// </summary>
        /// <param name="track">A <see cref="Track" /></param>
        /// <returns>A <see cref="RatingResponse" /></returns>
        /// <remarks>
        ///     The <see cref="Track" /> passed in must be a "Corrected Track" as
        ///     returned in <see cref="ScrobbleResponse" /> or <see cref="NowPlayingResponse" /></remarks>
        public RatingResponse Love(Track track)
        {
            RatingResponse result = TrackApi.Love(track, Authentication);
            result.Track = track;
            return result;
        }

        /// <summary>
        ///     Submits a Track unLove request to the Last.fm web service
        /// </summary>
        /// <param name="track">A <see cref="Track" /></param>
        /// <returns>A <see cref="RatingResponse" /></returns>
        /// <remarks>
        ///     The <see cref="Track" /> passed in must be a "Corrected Track" as
        ///     returned in <see cref="ScrobbleResponse" /> or <see cref="NowPlayingResponse" /></remarks>
        public RatingResponse UnLove(Track track)
        {
            RatingResponse result = TrackApi.UnLove(track, Authentication);
            result.Track = track;
            return result;
        }

        /// <summary>
        ///     Submits a Track Ban request to the Last.fm web service
        /// </summary>
        /// <param name="track">A <see cref="Track" /></param>
        /// <returns>A <see cref="RatingResponse" /></returns>
        /// <remarks>
        ///     The <see cref="Track" /> passed in must be a "Corrected Track" as
        ///     returned in <see cref="ScrobbleResponse" /> or <see cref="NowPlayingResponse" /></remarks>
        public RatingResponse Ban(Track track)
        {
            RatingResponse result = TrackApi.Ban(track, Authentication);
            result.Track = track;
            return result;
        }

        /// <summary>
        ///     Submits a Track UnBan request to the Last.fm web service
        /// </summary>
        /// <param name="track">A <see cref="Track" /></param>
        /// <returns>A <see cref="RatingResponse" /></returns>
        /// <remarks>
        ///     The <see cref="Track" /> passed in must be a "Corrected Track" as
        ///     returned in <see cref="ScrobbleResponse" /> or <see cref="NowPlayingResponse" /></remarks>
        public RatingResponse UnBan(Track track)
        {
            RatingResponse result = TrackApi.UnBan(track, Authentication);
            result.Track = track;
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void GetAuthenticationToken()
        {
            if (AuthenticationToken != null && AuthenticationToken.IsValid()) return;

            AuthenticationToken = AuthApi.GetToken(Authentication);
        }
    }
}