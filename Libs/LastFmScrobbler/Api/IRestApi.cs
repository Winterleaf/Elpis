namespace Elpis.Lpfm.LastFmScrobbler.Api
{
    /// <summary>
    ///     Defines a simple Rest API
    /// </summary>
    internal interface IRestApi
    {
        /// <summary>
        ///     Sends a GET request to the REST service
        /// </summary>
        /// <param name="url">A fully qualified URL</param>
        /// <param name="queryItems">A Dictionary of request query items</param>
        /// <returns>A read-only XPath queryable <see cref="System.Xml.XPath.XPathNavigator" /></returns>
        System.Xml.XPath.XPathNavigator SendGetRequest(string url,
            System.Collections.Generic.Dictionary<string, string> queryItems);

        /// <summary>
        ///     Sends a POST request to the REST service
        /// </summary>
        /// <param name="url">A fully qualified URL</param>
        /// <param name="formItems">
        ///     A <see cref="System.Collections.Specialized.NameValueCollection" /> of form items to post in
        ///     the body of the request
        /// </param>
        /// <returns>A read-only XPath queryable <see cref="System.Xml.XPath.XPathNavigator" /></returns>
        System.Xml.XPath.XPathNavigator SendPostRequest(string url,
            System.Collections.Generic.Dictionary<string, string> formItems);
    }
}