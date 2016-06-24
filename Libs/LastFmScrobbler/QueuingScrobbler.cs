namespace Lpfm.LastFmScrobbler
{
    /// <summary>
    /// 
    /// </summary>
    public enum Rating
    {
        /// <summary>
        /// Ban
        /// </summary>
        Ban = 0,
        /// <summary>
        /// Love
        /// </summary>
        Love = 1,
        /// <summary>
        /// Unban
        /// </summary>
        Unban = 2,/// <summary>
        /// Unlove
        /// </summary>
        Unlove = 3
    }

    internal class RatingObject
    {
        public Track Track { get; set; }
        public Rating RatingType { get; set; }
    }

    /// <summary>
    ///     A Scrobbler object that scrobbles to a queue until the application is ready to process
    /// </summary>
    /// <remarks>Use this version of the Scrobbler as a helper for asynchronous scrobbling</remarks>
    public class QueuingScrobbler
    {
        /// <summary>
        ///     Instantiates an instance of a <see cref="QueuingScrobbler" />
        /// </summary>
        /// <param name="apiKey">Required. An API Key from Last.fm. See http://www.last.fm/api/account </param>
        /// <param name="apiSecret">Required. An API Secret from Last.fm. See http://www.last.fm/api/account </param>
        /// <param name="sessionKey">Required. An authorized Last.fm Session Key. See <see cref="Scrobbler.GetSession" /></param>
        /// <exception cref="System.ArgumentNullException" />
        public QueuingScrobbler(string apiKey, string apiSecret, string sessionKey)
        {
            if (string.IsNullOrEmpty(apiKey)) throw new System.ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrEmpty(apiSecret)) throw new System.ArgumentNullException(nameof(apiSecret));
            //if (string.IsNullOrEmpty(sessionKey)) throw new ArgumentNullException("sessionKey");

            ApiKey = apiKey;
            ApiSecret = apiSecret;
            SessionKey = sessionKey;
            NowPlayingQueue = new System.Collections.Concurrent.ConcurrentQueue<Track>();
            ScrobbleQueue = new System.Collections.Concurrent.ConcurrentQueue<Track>();
            RatingQueue = new System.Collections.Concurrent.ConcurrentQueue<RatingObject>();

            BaseScrobbler = new Scrobbler(ApiKey, ApiSecret, SessionKey);
        }

        private System.Collections.Concurrent.ConcurrentQueue<Track> ScrobbleQueue { get; }

        /// <summary>
        /// 
        /// </summary>
        public int ScrobbleQueueCount => ScrobbleQueue?.Count ?? 0;

        private System.Collections.Concurrent.ConcurrentQueue<Track> NowPlayingQueue { get; }

        /// <summary>
        /// 
        /// </summary>
        public int NowPlayingQueueCount => NowPlayingQueue?.Count ?? 0;

        private System.Collections.Concurrent.ConcurrentQueue<RatingObject> RatingQueue { get; }

        /// <summary>
        /// 
        /// </summary>
        public int RatingQueueCount => RatingQueue?.Count ?? 0;

        /// <summary>
        /// 
        /// </summary>
        public int QueuedCount => ScrobbleQueueCount + NowPlayingQueueCount + RatingQueueCount;

        /// <summary>
        /// 
        /// </summary>
        public Scrobbler BaseScrobbler { get; }

        private string ApiKey { get; }
        private string ApiSecret { get; }
        private string SessionKey { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proxy"></param>
        public static void SetWebProxy(System.Net.WebProxy proxy)
        {
            Api.WebRequestRestApi.SetWebProxy(proxy);
        }

        /// <summary>
        ///     Enqueues a NowPlaying request but does not send it. Call <see cref="Process" /> to send
        /// </summary>
        /// <param name="track">The <see cref="Track" /> that is now playing</param>
        /// <remarks>
        ///     This method is thread-safe. Will not check for invalid tracks until Processed. You should validate the Track
        ///     before calling NowPlaying
        /// </remarks>
        public void NowPlaying(Track track)
        {
            NowPlayingQueue.Enqueue(track);
        }

        /// <summary>
        ///     Enqueues a Srobble request but does not send it. Call <see cref="Process" /> to send
        /// </summary>
        /// <param name="track">The <see cref="Track" /> that has played</param>
        /// <remarks>
        ///     This method is thread-safe. Will not check for invalid tracks until Processed. You should validate the Track
        ///     before calling Scrobble
        /// </remarks>
        public void Scrobble(Track track)
        {
            ScrobbleQueue.Enqueue(track);
        }

        /// <summary>
        ///     Enqueues a Love request but does not send it. Call <see cref="Process" /> to send
        /// </summary>
        /// <param name="track">The <see cref="Track" /> that has played</param>
        /// <remarks>This method is thread-safe. Will not check for invalid tracks until Processed.</remarks>
        public void Love(Track track)
        {
            RatingQueue.Enqueue(new RatingObject {Track = track, RatingType = Rating.Love});
        }

        /// <summary>
        ///     Enqueues a UnLove request but does not send it. Call <see cref="Process" /> to send
        /// </summary>
        /// <param name="track">The <see cref="Track" /> that has played</param>
        /// <remarks>This method is thread-safe. Will not check for invalid tracks until Processed.</remarks>
        public void UnLove(Track track)
        {
            RatingQueue.Enqueue(new RatingObject {Track = track, RatingType = Rating.Unlove});
        }

        /// <summary>
        ///     Enqueues a Ban request but does not send it. Call <see cref="Process" /> to send
        /// </summary>
        /// <param name="track">The <see cref="Track" /> that has played</param>
        /// <remarks>This method is thread-safe. Will not check for invalid tracks until Processed.</remarks>
        public void Ban(Track track)
        {
            RatingQueue.Enqueue(new RatingObject {Track = track, RatingType = Rating.Ban});
        }

        /// <summary>
        ///     Enqueues a UnBan request but does not send it. Call <see cref="Process" /> to send
        /// </summary>
        /// <param name="track">The <see cref="Track" /> that has played</param>
        /// <remarks>This method is thread-safe. Will not check for invalid tracks until Processed.</remarks>
        public void UnBan(Track track)
        {
            RatingQueue.Enqueue(new RatingObject {Track = track, RatingType = Rating.Unban});
        }

        /// <summary>
        ///     Synchronously processes all scrobbles and now playing notifications that are in the Queues, and returns the results
        /// </summary>
        /// <param name="throwExceptionDuringProcess">
        ///     When true, will throw the first Exception encountered during Scrobbling (and cease to process).
        ///     When false, any exceptions raised will be attached to the corresponding <see cref="ScrobbleResponse" />, but will
        ///     not be thrown. Default is false.
        /// </param>
        /// <returns><see cref="ScrobbleResponses" />, a list of <see cref="ScrobbleResponse" /> </returns>
        /// <remarks>
        ///     This method will complete synchronously and may take some time. This should be invoked by a single timer. This
        ///     method may not be thread safe
        /// </remarks>
        public System.Collections.Generic.List<Response> Process(bool throwExceptionDuringProcess = false)
        {
            if (string.IsNullOrEmpty(SessionKey)) return null;
            System.Collections.Generic.List<Response> results = new System.Collections.Generic.List<Response>();

            Track track;
            while (NowPlayingQueue.TryDequeue(out track))
            {
                try
                {
                    results.Add(BaseScrobbler.NowPlaying(track));
                }
                catch (System.Exception exception)
                {
                    if (throwExceptionDuringProcess) throw;
                    results.Add(new NowPlayingResponse {Track = track, Exception = exception});
                }
            }

            while (ScrobbleQueue.TryDequeue(out track))
            {
                //TODO: Implement bulk scrobble
                try
                {
                    results.Add(BaseScrobbler.Scrobble(track));
                }
                catch (System.Exception exception)
                {
                    if (throwExceptionDuringProcess) throw;
                    results.Add(new ScrobbleResponse {Track = track, Exception = exception});
                }
            }

            RatingObject rating;
            while (RatingQueue.TryDequeue(out rating))
            {
                try
                {
                    switch (rating.RatingType)
                    {
                        case Rating.Love:
                            results.Add(BaseScrobbler.Love(rating.Track));
                            break;
                        case Rating.Ban:
                            results.Add(BaseScrobbler.Ban(rating.Track));
                            break;
                        case Rating.Unlove:
                            results.Add(BaseScrobbler.UnLove(rating.Track));
                            break;
                        case Rating.Unban:
                            results.Add(BaseScrobbler.UnBan(rating.Track));
                            break;
                        default:
                            throw new System.ArgumentOutOfRangeException();
                    }
                }
                catch (System.Exception exception)
                {
                    if (throwExceptionDuringProcess) throw;
                    results.Add(new RatingResponse {ErrorCode = -1, Exception = exception, Track = rating.Track});
                }
            }

            return results;
        }
    }
}