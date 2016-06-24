using HttpUtility = Elpis.Lpfm.LastFmScrobbler.HttpUtility;

namespace Elpis.Lpfm.LastFmScrobbler.Api

{
    /// <summary>
    ///     An implementation of <see cref="IRestApi" /> based on <see cref="System.Net.HttpWebRequest" />
    /// </summary>
    internal class WebRequestRestApi : IRestApi
    {
        /// <summary>
        ///     The Name Value Pair Format String used by this object
        /// </summary>
        public const string NameValuePairStringFormat = "{0}={1}";

        /// <summary>
        ///     The Name-value pair seperator used by this object
        /// </summary>
        public const string NameValuePairStringSeperator = "&";

        private static System.Net.WebProxy _proxy;

        public static void SetWebProxy(System.Net.WebProxy proxy)
        {
            _proxy = proxy;
        }

        protected virtual string BuildStringOfItems(System.Collections.Generic.Dictionary<string, string> queryItems)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            int count = 0;
            foreach (System.Collections.Generic.KeyValuePair<string, string> nameValue in queryItems)
            {
                if (count > 0) builder.Append(NameValuePairStringSeperator);
                builder.AppendFormat(NameValuePairStringFormat, nameValue.Key,
                    HttpUtility.UrlEncode(nameValue.Value));
                count++;
            }

            return builder.ToString();
        }

        protected virtual bool TryGetXpathDocumentFromResponse(System.Net.WebResponse response,
            out System.Xml.XPath.XPathNavigator document)
        {
            bool parsed;

            try
            {
                document = GetXpathDocumentFromResponse(response);
                parsed = true;
            }
            catch (System.Exception)
            {
                document = null;
                parsed = false;
            }

            return parsed;
        }

        protected virtual System.Xml.XPath.XPathNavigator GetXpathDocumentFromResponse(System.Net.WebResponse response)
        {
            using (System.IO.Stream stream = response.GetResponseStream())
            {
                if (stream == null) throw new System.InvalidOperationException("Response Stream is null");

                try
                {
                    return new System.Xml.XPath.XPathDocument(stream).CreateNavigator();
                }
                catch (System.Xml.XmlException exception)
                {
                    throw new System.Xml.XmlException("Could not read HTTP Response as XML", exception);
                }
                finally
                {
                    response.Close();
                }
            }
        }

        protected virtual System.Net.WebRequest CreateWebRequest(System.Uri uri)
        {
            System.Net.WebRequest request = System.Net.WebRequest.Create(uri);
            if (_proxy != null)
                request.Proxy = _proxy;

            return request;
        }

        protected virtual System.Net.WebRequest CreateWebRequest(string uri)
        {
            return CreateWebRequest(new System.Uri(uri));
        }

        #region IRestApi Members

        /// <summary>
        ///     Sends a GET request to the REST service
        /// </summary>
        /// <param name="url">A fully qualified URL</param>
        /// <param name="queryItems">A Dictionary of request query items</param>
        /// <returns>A read-only XPath queryable <see cref="System.Xml.XPath.XPathNavigator" /></returns>
        public System.Xml.XPath.XPathNavigator SendGetRequest(string url,
            System.Collections.Generic.Dictionary<string, string> queryItems)
        {
            if (string.IsNullOrEmpty(url)) throw new System.ArgumentNullException(nameof(url));
            if (queryItems == null) throw new System.ArgumentNullException(nameof(queryItems));

            System.UriBuilder builder = new System.UriBuilder(url) {Query = BuildStringOfItems(queryItems)};

            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.WebRequest request = CreateWebRequest(builder.Uri);
            request.Method = "GET";

            return GetResponseAsXml(request);
        }

        /// <summary>
        ///     Synchronously sends a POST request to the REST service and returns the XML Response
        /// </summary>
        /// <param name="url">A fully qualified URL</param>
        /// <param name="formItems">
        ///     A <see cref="System.Collections.Specialized.NameValueCollection" /> of name-value pairs to post
        ///     in the body of the request
        /// </param>
        /// <returns>A read-only XPath queryable <see cref="System.Xml.XPath.XPathNavigator" /></returns>
        /// <remarks>Will synchronously HTTP POST a application/x-www-form-urlencoded request</remarks>
        public System.Xml.XPath.XPathNavigator SendPostRequest(string url,
            System.Collections.Generic.Dictionary<string, string> formItems)
        {
            if (string.IsNullOrEmpty(url)) throw new System.ArgumentNullException(nameof(url));
            if (formItems == null) throw new System.ArgumentNullException(nameof(formItems));

            // http://msdn.microsoft.com/en-us/library/system.net.servicepointmanager.expect100continue.aspx
            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.WebRequest request = CreateWebRequest(url);
            request.Method = "POST";

            string postData = BuildStringOfItems(formItems);

            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            using (System.IO.Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
            }

            return GetResponseAsXml(request);
        }

        protected internal virtual System.Xml.XPath.XPathNavigator GetResponseAsXml(System.Net.WebRequest request)
        {
            System.Net.WebResponse response;
            System.Xml.XPath.XPathNavigator navigator;
            try
            {
                response = request.GetResponse();
                navigator = GetXpathDocumentFromResponse(response);
                ApiHelper.CheckLastFmStatus(navigator);
            }
            catch (System.Net.WebException exception)
            {
                response = exception.Response;

                System.Xml.XPath.XPathNavigator document;
                TryGetXpathDocumentFromResponse(response, out document);

                if (document != null) ApiHelper.CheckLastFmStatus(document, exception);
                throw; // throw even if Last.fm status is OK
            }

            return navigator;
        }

        #endregion
    }
}