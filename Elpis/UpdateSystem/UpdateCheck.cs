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

namespace Elpis.UpdateSystem
{
    public class UpdateCheck
    {
        public System.Version CurrentVersion => System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

        public System.Version NewVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleaseNotesPath { get; set; }
        public string ReleaseNotes { get; set; }
        public bool UpdateNeeded { get; set; }
        public string UpdatePath { get; set; }

        private bool _downloadComplete;
        private string _downloadString = string.Empty;
        public event UpdateDataLoadedEventHandler UpdateDataLoadedEvent;

        private void SendUpdateEvent(bool foundUpdate)
        {
            UpdateDataLoadedEvent?.Invoke(foundUpdate);
        }

        private string DownloadString(string url, int timeoutSec = 10)
        {
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                wc.DownloadStringCompleted += wc_DownloadStringCompleted;

                _downloadComplete = false;
                _downloadString = string.Empty;
                if (Util.PRequest.Proxy != null)
                    wc.Proxy = Util.PRequest.Proxy;

                wc.DownloadStringAsync(new System.Uri(url));

                System.DateTime start = System.DateTime.Now;
                while (!_downloadComplete && ((System.DateTime.Now - start).TotalMilliseconds < timeoutSec*1000))
                    System.Threading.Thread.Sleep(25);

                if (_downloadComplete)
                    return _downloadString;

                wc.CancelAsync();

                throw new System.Exception("Timeout waiting for " + url + " to download.");
            }
        }

        private void wc_DownloadStringCompleted(object sender, System.Net.DownloadStringCompletedEventArgs e)
        {
            try
            {
                _downloadString = e.Result;
            }
            catch
            {
                _downloadString = string.Empty;
            }

            _downloadComplete = true;
        }

        private bool CheckForUpdateInternal(bool beta = false)
        {
            try
            {
                Util.Log.O("Checking for " + (beta ? "beta " : "") + "updates...");
                string updateUrl = "";

#if APP_RELEASE
                updateUrl = ReleaseData.UpdateBaseUrl + ReleaseData.UpdateConfigFile +
                            "?r=" + DateTime.UtcNow.ToEpochTime().ToString();
                    //Because WebClient won't let you disable caching :(
#endif
                Util.Log.O("Downloading update file: " + updateUrl);

                string data = DownloadString(updateUrl);

                Util.MapConfig mc = new Util.MapConfig();
                mc.LoadConfig(data);

                string verStr = mc.GetValue(beta ? "BetaVersion" : "CurrentVersion", string.Empty);
                DownloadUrl = mc.GetValue(beta ? "BetaDownloadUrl" : "DownloadUrl", string.Empty);
                ReleaseNotesPath = mc.GetValue(beta ? "BetaReleaseNotes" : "ReleaseNotes", string.Empty);

                ReleaseNotes = string.Empty;
                if (ReleaseNotesPath != string.Empty)
                {
                    ReleaseNotes = DownloadString(ReleaseNotesPath);
                }

                System.Version ver;

                if (!System.Version.TryParse(verStr, out ver))
                {
                    SendUpdateEvent(false);
                    return false;
                }

                NewVersion = ver;

                bool result = NewVersion > CurrentVersion;

                UpdateNeeded = result;
                SendUpdateEvent(result);
                return result;
            }
            catch (System.Exception e)
            {
                Util.Log.O("Error checking for updates: " + e);
                UpdateNeeded = false;
                SendUpdateEvent(false);
                return false;
            }
        }

        public bool CheckForUpdate()
        {
            return CheckForUpdateInternal();
        }

        public bool CheckForBetaUpdate()
        {
            return CheckForUpdateInternal(true);
        }

        public void CheckForUpdateAsync()
        {
            System.Threading.Tasks.Task.Factory.StartNew(() => CheckForUpdateInternal());
        }

        public void DownloadUpdateAsync(string outputPath)
        {
            UpdatePath = outputPath;
            Util.Log.O("Download Elpis Update...");
            System.Threading.Tasks.Task.Factory.StartNew(
                () =>
                    Util.PRequest.FileRequestAsync(DownloadUrl, outputPath, DownloadProgressChanged,
                        DownloadFileCompleted));
        }

        private void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
                Util.Log.O("Update Download Complete.");
            else
                Util.Log.O("Update Download Error: " + e.Error);

            DownloadComplete?.Invoke(e.Error != null, e.Error);
        }

        private void DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            DownloadProgress?.Invoke(e.ProgressPercentage);
        }

        #region Delegates

        public delegate void UpdateDataLoadedEventHandler(bool foundUpdate);

        public delegate void DownloadProgressHandler(int prog);

        public event DownloadProgressHandler DownloadProgress;

        public delegate void DownloadCompleteHandler(bool error, System.Exception ex);

        public event DownloadCompleteHandler DownloadComplete;

        #endregion
    }
}