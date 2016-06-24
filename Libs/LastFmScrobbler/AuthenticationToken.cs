namespace Elpis.Lpfm.LastFmScrobbler
{
    /// <summary>
    ///     A Last.fm Authentication Token DTO
    /// </summary>
    public class AuthenticationToken
    {
        /// <summary>
        ///     Instantiates an <see cref="AuthenticationToken" />
        /// </summary>
        /// <param name="value"></param>
        public AuthenticationToken(string value)
        {
            Created = System.DateTime.Now;
            Value = value;
        }

        /// <summary>
        ///     The number of seconds a token is valid for
        /// </summary>
        public const int ValidForMinutes = 60;

        /// <summary>
        ///     The token string value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        ///     When the token was created
        /// </summary>
        public System.DateTime Created { get; protected set; }

        /// <summary>
        ///     True when this token is still valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return Created > System.DateTime.Now.AddMinutes(-ValidForMinutes);
        }
    }
}