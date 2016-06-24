/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * Elpis is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/

namespace Util
{
    public class PostSubmitter
    {
        public PostSubmitter(string url)
        {
            _url = url;
            _url = _url + "?sync=" + System.DateTime.UtcNow.ToEpochTime(); //randomize URL to prevent caching
            _params = new System.Collections.Specialized.NameValueCollection();
        }

        private readonly System.Collections.Specialized.NameValueCollection _params;
        private readonly string _url;

        private bool _uploadComplete;
        private string _uploadResult = string.Empty;

        public void Add(string key, string value)
        {
            _params.Add(key, value);
        }

        public string Send(int timeoutSec = 10)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            wc.UploadValuesCompleted += wc_UploadValuesCompleted;

            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            wc.Headers.Add("Origin", "http://elpis.adamhaile.net");

            if (PRequest.Proxy != null)
                wc.Proxy = PRequest.Proxy;

            wc.UploadValuesAsync(new System.Uri(_url), "POST", _params);

            System.DateTime start = System.DateTime.Now;
            while (!_uploadComplete && ((System.DateTime.Now - start).TotalMilliseconds < timeoutSec*1000))
                System.Threading.Thread.Sleep(25);

            if (_uploadComplete)
                return _uploadResult;

            wc.CancelAsync();

            throw new System.Exception("Timeout waiting for POST to " + _url);
        }

        private void wc_UploadValuesCompleted(object sender, System.Net.UploadValuesCompletedEventArgs e)
        {
            _uploadResult = System.Text.Encoding.ASCII.GetString(e.Result);
            _uploadComplete = true;
        }
    }
}