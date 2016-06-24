using Enumerable = System.Linq.Enumerable;

namespace Elpis.Lpfm.LastFmScrobbler.Api
{
    internal class ApiHelper
    {
        private const string LastFmStatusOk = "ok";
        private const string LastFmErrorXPath = "/lfm/error";
        private const string LastFmErrorCodeXPath = "/lfm/error/@code";
        private const string LastFmStatusXPath = "/lfm/@status";

        /// <summary>
        ///     The Last.fm web service root URL
        /// </summary>
        public const string LastFmWebServiceRootUrl = "http://ws.audioscrobbler.com/2.0/";

        public const string MethodParamName = "method";
        public const string ApiKeyParamName = "api_key";
        public const string ApiSignatureParamName = "api_sig";
        public const string SessionKeyParamName = "sk";

        /// <summary>
        ///     Check the Last.fm status of the response and throw a <see cref="LastFmApiException" /> if an error is detected
        /// </summary>
        /// <param name="navigator">The response as <see cref="System.Xml.XPath.XPathNavigator" /></param>
        /// <exception cref="LastFmApiException" />
        public static void CheckLastFmStatus(System.Xml.XPath.XPathNavigator navigator)
        {
            CheckLastFmStatus(navigator, null);
        }

        /// <summary>
        ///     Check the Last.fm status of the response and throw a <see cref="LastFmApiException" /> if an error is detected
        /// </summary>
        /// <param name="navigator">The response as <see cref="System.Xml.XPath.XPathNavigator" /></param>
        /// <param name="webException">An optional <see cref="System.Net.WebException" /> to be set as the inner exception</param>
        /// <exception cref="LastFmApiException" />
        public static void CheckLastFmStatus(System.Xml.XPath.XPathNavigator navigator,
            System.Net.WebException webException)
        {
            System.Xml.XPath.XPathNavigator node = SelectSingleNode(navigator, LastFmStatusXPath);

            if (node.Value == LastFmStatusOk) return;

            throw new LastFmApiException($"LastFm status = \"{node.Value}\". Error code = {SelectSingleNode(navigator, LastFmErrorCodeXPath)}. {SelectSingleNode(navigator, LastFmErrorXPath)}",
                webException);
        }

        /// <summary>
        ///     Helper method to select a single node from an <see cref="System.Xml.XPath.XPathNavigator" /> as
        ///     <see cref="System.Xml.XPath.XPathNavigator" />
        /// </summary>
        public static System.Xml.XPath.XPathNavigator SelectSingleNode(System.Xml.XPath.XPathNavigator navigator,
            string xpath)
        {
            System.Xml.XPath.XPathNavigator node = navigator.SelectSingleNode(xpath);
            if (node == null)
                throw new System.InvalidOperationException(
                    "Node is null. Cannot select single node. XML response may be mal-formed");
            return node;
        }

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// 
        ///     Adds the parameters that are required by the Last.Fm API to the <see cref="parameters" /> dictionary
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="methodName"></param>
        /// <param name="authentication"></param>
        /// <param name="addApiSignature"></param>
        public static void AddRequiredParams(System.Collections.Generic.Dictionary<string, string> parameters,
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
            string methodName, Authentication authentication, bool addApiSignature = true)
        {
            // method
            parameters.Add(MethodParamName, methodName);

            // api key
            parameters.Add(ApiKeyParamName, authentication.ApiKey);

            // session key
            if (authentication.Session != null) parameters.Add(SessionKeyParamName, authentication.Session.Key);

            // api_sig
            if (addApiSignature)
            {
                parameters.Add(ApiSignatureParamName, GetApiSignature(parameters, authentication.ApiSecret));
            }
        }

        /// <summary>
        ///     Generates a hashed Last.fm API Signature from the parameter name-value pairs, and the API secret
        /// </summary>
        public static string GetApiSignature(System.Collections.Generic.Dictionary<string, string> nameValues,
            string apiSecret)
        {
            string parameters = GetStringOfOrderedParamsForHashing(nameValues);
            parameters += apiSecret;

            return Hash(parameters);
        }

        /// <summary>
        ///     Gets a string of ordered parameter values for hashing
        /// </summary>
        public static string GetStringOfOrderedParamsForHashing(
            System.Collections.Generic.Dictionary<string, string> nameValues)
        {
            System.Text.StringBuilder paramsBuilder = new System.Text.StringBuilder();

            foreach (
                System.Collections.Generic.KeyValuePair<string, string> nameValue in
                    Enumerable.OrderBy(nameValues, nv => nv.Key))
            {
                paramsBuilder.Append($"{nameValue.Key}{nameValue.Value}");
            }
            return paramsBuilder.ToString();
        }

        // http://msdn.microsoft.com/en-us/library/system.security.cryptography.md5.aspx
        // Hash an input string and return the hash as
        // a 32 character hexadecimal string.
        public static string Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            System.Security.Cryptography.MD5 md5Hasher = System.Security.Cryptography.MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(System.Text.Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            System.Text.StringBuilder sBuilder = new System.Text.StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            foreach (byte t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}