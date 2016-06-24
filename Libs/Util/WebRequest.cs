/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of PandoraSharp.
 * PandoraSharp is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * PandoraSharp is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with PandoraSharp. If not, see http://www.gnu.org/licenses/.
*/

namespace Util
{
    public class PRequest
    {
        private static readonly string _userAgent =
            "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/535.7 (KHTML, like Gecko) Chrome/16.0.912.63 Safari/535.7";

        public static System.Net.WebProxy Proxy { get; private set; }

        public static void SetProxy(string address, string user = "", string password = "")
        {
            System.Net.WebProxy p = new System.Net.WebProxy(new System.Uri(address));

            if (user != "")
                p.Credentials = new System.Net.NetworkCredential(user, password);

            Proxy = p;
        }

        public static void SetProxy(string address, int port, string user = "", string password = "")
        {
            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.WebProxy p = new System.Net.WebProxy(address, port);

            if (user != "")
                p.Credentials = new System.Net.NetworkCredential(user, password);

            Proxy = p;
        }

        public static string StringRequest(string url, string data)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            if (Proxy != null)
                wc.Proxy = Proxy;

            wc.Encoding = System.Text.Encoding.UTF8;
            wc.Headers.Add("Content-Type", "text/plain; charset=utf8");
            wc.Headers.Add("User-Agent", _userAgent);

            string response;
            try
            {
                response = wc.UploadString(new System.Uri(url), "POST", data);
            }
            catch (System.Net.WebException wex)
            {
                Log.O("StringRequest Error: " + wex);
                //Wait and Try again, just in case
                System.Threading.Thread.Sleep(500);
                response = wc.UploadString(new System.Uri(url), "POST", data);
            }

            //Log.O(response);
            return response;
        }

        public static void ByteRequestAsync(string url, System.Net.DownloadDataCompletedEventHandler dataHandler)
        {
            Log.O("Downloading Async: " + url);
            System.Net.WebClient wc = new System.Net.WebClient();
            if (Proxy != null)
                wc.Proxy = Proxy;

            wc.DownloadDataCompleted += dataHandler;
            wc.DownloadDataAsync(new System.Uri(url));
        }

        public static byte[] ByteRequest(string url)
        {
            Log.O("Downloading: " + url);
            System.Net.WebClient wc = new System.Net.WebClient();
            if (Proxy != null)
                wc.Proxy = Proxy;

            return wc.DownloadData(new System.Uri(url));
        }

        public static void FileRequest(string url, string outputFile)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            if (Proxy != null)
                wc.Proxy = Proxy;
            wc.DownloadFile(url, outputFile);
        }

        public static void FileRequestAsync(string url, string outputFile,
            System.Net.DownloadProgressChangedEventHandler progressCallback,
            System.ComponentModel.AsyncCompletedEventHandler completeCallback)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            if (Proxy != null)
                wc.Proxy = Proxy;

            wc.DownloadFileCompleted += completeCallback;
            wc.DownloadProgressChanged += progressCallback;
            wc.DownloadFileAsync(new System.Uri(url), outputFile);
        }
    }
}