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

using Enumerable = System.Linq.Enumerable;

namespace Elpis.PandoraSharp
{
    public class Station : System.ComponentModel.INotifyPropertyChanged
    {
        public Station(Pandora p, Newtonsoft.Json.Linq.JToken d)
        {
            SkipLimitReached = false;
            SkipLimitTime = System.DateTime.MinValue;

            _pandora = p;

            Id = d["stationId"].ToString();
            IdToken = d["stationToken"].ToString();
            IsCreator = !d["isShared"].ToObject<bool>();
            IsQuickMix = d["isQuickMix"].ToObject<bool>();
            Name = d["stationName"].ToString();
            InfoUrl = (string) d["stationDetailUrl"];

            if (IsQuickMix)
            {
                Name = "Quick Mix";
                _pandora.QuickMixStationIDs.Clear();
                string[] qmIDs = d["quickMixStationIds"].ToObject<string[]>();
                foreach (string qmid in qmIDs)
                    _pandora.QuickMixStationIDs.Add(qmid);
            }

            bool downloadArt = true;
            if (!_pandora.ImageCachePath.Equals("") && System.IO.File.Exists(ArtCacheFile))
            {
                try
                {
                    ArtImage = System.IO.File.ReadAllBytes(ArtCacheFile);
                }
                catch (System.Exception)
                {
                    Util.Log.O("Error retrieving image cache file: " + ArtCacheFile);
                }

                downloadArt = false;
            }

            if (!downloadArt) return;
            Newtonsoft.Json.Linq.JToken value = d.SelectToken("artUrl");
            if (value == null) return;
            ArtUrl = value.ToString();

            if (ArtUrl == string.Empty) return;
            try
            {
                ArtImage = Util.PRequest.ByteRequest(ArtUrl);
                if (ArtImage.Length > 0)
                    System.IO.File.WriteAllBytes(ArtCacheFile, ArtImage);
            }
            catch (System.Exception)
            {
                Util.Log.O("Error saving image cache file: " + ArtCacheFile);
            }
            //}
        }

        public string Id { get; }
        public string IdToken { get; }
        public bool IsCreator { get; private set; }

        [System.ComponentModel.DefaultValue(false)]
        public bool IsQuickMix { get; }

        public string Name { get; private set; }

        public bool UseQuickMix
        {
            get { return _useQuickMix; }
            set
            {
                if (value == _useQuickMix) return;
                _useQuickMix = value;
                Notify("UseQuickMix");
            }
        }

        public string ArtUrl { get; }

        [System.Xml.Serialization.XmlIgnore]
        [System.Web.Script.Serialization.ScriptIgnore]
        public byte[] ArtImage
        {
            get
            {
                lock (_artLock)
                {
                    return _artImage;
                }
            }
            private set
            {
                lock (_artLock)
                {
                    _artImage = value;
                }
            }
        }

        public string InfoUrl { get; set; }

        public int ThumbsUp { get; set; }
        public int ThumbsDown { get; set; }

        public string ArtCacheFile => System.IO.Path.Combine(_pandora.ImageCachePath, "Station_" + IdToken);

        public bool SkipLimitReached { get; set; }
        public System.DateTime SkipLimitTime { get; set; }
        private readonly object _artLock = new object();
        private readonly Pandora _pandora;
        private byte[] _artImage;

        private bool _gettingPlaylist;

        private bool _useQuickMix;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void Notify(string info)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(info));
        }

        //private void StationArtDownloadHandler(object sender, System.Net.DownloadDataCompletedEventArgs e)
        //{
        //    if (e.Result.Length == 0)
        //        return;

        //    ArtImage = e.Result;
        //}

        public void TransformIfShared()
        {
            if (IsCreator) return;
            Util.Log.O("Pandora: Transforming Station");
            _pandora.CallRPC("station.transformSharedStation", "stationToken", IdToken);
            IsCreator = true;
        }

        public System.Collections.Generic.List<Song> GetPlaylist()
        {
            System.Collections.Generic.List<Song> results = new System.Collections.Generic.List<Song>();
            if (_gettingPlaylist) return results;
            Util.Log.O("GetPlaylist");
            try
            {
                _gettingPlaylist = true;
                Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["stationToken"] = IdToken};
                if (_pandora.AudioFormat != PAudioFormat.AacPlus)
                    req["additionalAudioUrl"] = "HTTP_128_MP3,HTTP_192_MP3";

                JsonResult playlist = _pandora.CallRPC("station.getPlaylist", req, false, true); // MUST use SSL

                foreach (Newtonsoft.Json.Linq.JToken song in Enumerable.Where(playlist.Result["items"], song => song["songName"] != null)) {
                    try
                    {
                        results.Add(new Song(_pandora, song));
                    }
                    catch (PandoraException ex)
                    {
                        Util.Log.O("Song Add Error: " + ex.FaultMessage);
                    }
                }

                _gettingPlaylist = false;
                return results;
            }
            catch (PandoraException ex)
            {
                _gettingPlaylist = false;
                if (ex.Message == "PLAYLIST_END" || ex.Message == "DAILY_SKIP_LIMIT_REACHED")
                {
                    if (ex.Message == "PLAYLIST_END")
                    {
                        SkipLimitReached = true;
                        SkipLimitTime = System.DateTime.Now;
                    }
                    else
                        throw;
                }

                Util.Log.O("Error getting playlist, will try again next time: " + Util.Errors.GetErrorMessage(ex.Fault));
                return results;
            }
        }

        public void AddVariety(SearchResult item)
        {
            Util.Log.O("Pandora: Adding {0} to {1}", item.DisplayName, Name);

            try
            {
                _pandora.CallRPC("station.addMusic", "stationToken", IdToken, "musicToken", item.MusicToken);
            }
            catch
            {
                //
            } // eventually do something with this
        }

        public void Rename(string newName)
        {
            try
            {
                TransformIfShared();
                if (newName == Name)
                    return;
                Util.Log.O("Pandora: Renaming Station");
                _pandora.CallRPC("station.renameStation", "stationToken", IdToken, "stationName", newName);

                Name = newName;
            }
            catch (System.Exception ex)
            {
                Util.Log.O(ex.ToString());
            }
        }

        public void Delete()
        {
            Util.Log.O("Pandora: Deleting Station");
            _pandora.CallRPC("station.deleteStation", "stationToken", IdToken);
            if (!System.IO.File.Exists(ArtCacheFile)) return;
            try
            {
                System.IO.File.Delete(ArtCacheFile);
            }
            catch (System.Exception)
            {
                //todo
            }
        }

        public void CreateShortcut()
        {
            IWshRuntimeLibrary.WshShellClass wsh = new IWshRuntimeLibrary.WshShellClass();

            string targetPathWithoutExtension =
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\Elpis - " + Name;
            for (int i = 1; System.IO.File.Exists(targetPathWithoutExtension + ".lnk"); i++)
            {
                targetPathWithoutExtension = targetPathWithoutExtension + i;
            }
            IWshRuntimeLibrary.IWshShortcut shortcut =
                (IWshRuntimeLibrary.IWshShortcut) wsh.CreateShortcut(targetPathWithoutExtension + ".lnk");
            if (shortcut == null) return;
            shortcut.Arguments = $"--station={Id}";
            shortcut.TargetPath =
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                "\\Elpis.exe";
            // not sure about what this is for
            shortcut.WindowStyle = 1;
            shortcut.Description = $"Start Elpis tuned to {Name}";
            shortcut.WorkingDirectory =
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            //Get the assembly.
            System.Reflection.Assembly currAssembly = System.Reflection.Assembly.LoadFrom(shortcut.TargetPath);

            //Gets the image from the exe resources
            System.IO.Stream stream = currAssembly.GetManifestResourceStream("main_icon.ico");
            if (null != stream)
            {
                string temp = System.IO.Path.GetTempFileName();
                System.Drawing.Image.FromStream(stream).Save(temp);
                shortcut.IconLocation = temp;
            }

            shortcut.Save();
        }

        public System.Windows.Shell.JumpTask AsJumpTask()
        {
            System.Windows.Shell.JumpTask task = new System.Windows.Shell.JumpTask
            {
                Title = Name,
                Description = "Play station " + Name,
                ApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location,
                Arguments = "--station=" + Id
            };
            return task;
        }
    }
}