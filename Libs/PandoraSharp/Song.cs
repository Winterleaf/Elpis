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

namespace Elpis.PandoraSharp
{
    public class Song
    {
        public Song(Pandora p, Newtonsoft.Json.Linq.JToken song)
        {
            _metaDict = new System.Collections.Generic.Dictionary<object, object>();

            _pandora = p;

            TrackToken = (string) song["trackToken"];
            Artist = (string) song["artistName"];
            Album = (string) song["albumName"];

            AmazonAlbumId = (string) song["amazonAlbumDigitalAsin"];
            AmazonTrackId = (string) song["amazonSongDigitalAsin"];
            AmazonAlbumUrl = (string) song["amazonAlbumUrl"];

            string aacUrl = string.Empty;
            try
            {
                aacUrl = (string) song["audioUrlMap"]["highQuality"]["audioUrl"];
            }
            catch
            {
                //todo
            }

            if (_pandora.AudioFormat == PAudioFormat.AacPlus)
            {
                if (aacUrl == string.Empty)
                    throw new PandoraException(Util.ErrorCodes.NoAudioUrls);

                AudioUrl = aacUrl;
            }
            else
            {
                string[] songUrls = null;
                try
                {
                    songUrls = song["additionalAudioUrl"].HasValues ? song["additionalAudioUrl"].ToObject<string[]>() : new[] {(string) song["additionalAudioUrl"]};
                }
                catch
                {
                    //
                }

                if (songUrls == null || songUrls.Length == 0)
                {
                    if (aacUrl != string.Empty) AudioUrl = aacUrl;
                    else throw new PandoraException(Util.ErrorCodes.NoAudioUrls);
                }
                else if (songUrls.Length == 1)
                {
                    AudioUrl = songUrls[0];
                }
                else if (songUrls.Length > 1)
                {
                    if (_pandora.AudioFormat == PAudioFormat.Mp3Hifi)
                    {
                        AudioUrl = songUrls.Length >= 2 ? songUrls[1] : songUrls[0];
                    }
                    else //default to PAudioFormat.MP3
                    {
                        AudioUrl = songUrls[0];
                    }
                }
            }

            double gain;
            double.TryParse((string) song["trackGain"], out gain);
            FileGain = gain;

            Rating = (int) song["songRating"] > 0 ? SongRating.Love : SongRating.None;
            StationId = (string) song["stationId"];
            SongTitle = (string) song["songName"];
            SongDetailUrl = (string) song["songDetailUrl"];
            ArtistDetailUrl = (string) song["artistDetailUrl"];
            AlbumDetailUrl = (string) song["albumDetailUrl"];
            AlbumArtUrl = (string) song["albumArtUrl"];

            Tired = false;
            StartTime = System.DateTime.MinValue;
            Finished = false;
            PlaylistTime = Time.Unix();

            if (AlbumArtUrl.IsNullOrEmpty()) return;
            try
            {
                AlbumImage = Util.PRequest.ByteRequest(AlbumArtUrl);
            }
            catch
            {
                //todo
            }
        }

        public string FileName { get; set; }

        public bool Played { get; set; }

        public string Album { get; private set; }
        public string Artist { get; }
        public string AudioUrl { get; private set; }
        public double FileGain { get; private set; }
        public SongRating Rating { get; private set; }

        public bool Loved => Rating == SongRating.Love;

        public bool Banned => Rating == SongRating.Ban;

        public string RatingString => Rating.ToString();

        public string StationId { get; }
        public string TrackToken { get; }
        public string SongTitle { get; }
        public string SongDetailUrl { get; private set; }
        public string ArtistDetailUrl { get; set; }
        public string AlbumDetailUrl { get; private set; }
        public string AlbumArtUrl { get; }

        public string AmazonAlbumId { get; private set; }
        public string AmazonAlbumUrl { get; private set; }
        public string AmazonTrackId { get; private set; }

        [System.Xml.Serialization.XmlIgnore]
        [System.Web.Script.Serialization.ScriptIgnore]
        public byte[] AlbumImage
        {
            get
            {
                lock (_albumLock)
                {
                    return _albumImage;
                }
            }
            private set
            {
                lock (_albumLock)
                {
                    _albumImage = value;
                }
            }
        }

        //public int SongType { get; private set; }

        public bool Tired { get; private set; }
        public System.DateTime StartTime { get; private set; }
        public bool Finished { get; private set; }
        public int PlaylistTime { get; }

        public Station Station => _pandora.GetStationById(StationId);

        private string FeedbackId => _pandora.GetFeedbackId(StationId, TrackToken);

        public bool IsStillValid => Time.Unix() - PlaylistTime < Const.PlaylistValidityTime;

        private readonly object _albumLock = new object();
        private readonly Pandora _pandora;

        private readonly object _metaLock = new object();
        private byte[] _albumImage;
        private readonly System.Collections.Generic.Dictionary<object, object> _metaDict;

        public void SetMetaObject(object key, object value)
        {
            lock (_metaLock)
            {
                _metaDict[key] = value;
            }
        }

        public object GetMetaObject(object key)
        {
            lock (_metaLock)
            {
                return _metaDict.ContainsKey(key) ? _metaDict[key] : null;
            }
        }

        //private void AlbumArtDownloadHandler(object sender, System.Net.DownloadDataCompletedEventArgs e)
        //{
        //    //if error or zero length, we don't care, empty image
        //    if (e.Error != null || e.Result.Length == 0)
        //        return;

        //    AlbumImage = e.Result;
        //}

        public void Rate(SongRating rating)
        {
            if (Rating == rating) return;
            try
            {
                Station.TransformIfShared();
                if (rating == SongRating.None)
                    _pandora.DeleteFeedback(FeedbackId);
                else
                    _pandora.AddFeedback(Station.IdToken, TrackToken, rating);

                Rating = rating;
                _pandora.CallFeedbackUpdateEvent(this, true);
            }
            catch (System.Exception ex)
            {
                Util.Log.O(ex.ToString());
                _pandora.CallFeedbackUpdateEvent(this, false);
            }
        }

        public void SetTired()
        {
            if (!Tired)
            {
                try
                {
                    _pandora.CallRPC("user.sleepSong", "trackToken", TrackToken);
                    Tired = true;
                }
                catch
                {
                    //TODO: Give this a failed event to notify UI
                }
            }
        }

        public void Bookmark()
        {
            try
            {
                _pandora.CallRPC("bookmark.addSongBookmark", "trackToken", TrackToken);
            }
            catch
            {
                //TODO: Give this a failed event to notify UI
            }
        }

        public void BookmarkArtist()
        {
            try
            {
                _pandora.CallRPC("bookmark.addArtistBookmark", "trackToken", TrackToken);
            }
            catch
            {
                //TODO: Give this a failed event to notify UI
            }
        }

        public void CreateStation() {}

        public override string ToString()
        {
            return Artist + " - " + SongTitle;
        }
    }
}