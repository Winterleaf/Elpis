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

using System.Linq;

namespace Elpis.PandoraSharp
{
    public class Pandora
    {
        //private string webAuthToken;

        public Pandora()
        {
            QuickMixStationIDs = new System.Collections.Generic.List<string>();
            StationSortOrder = SortOrder.DateDesc;
            HasSubscription = true;
            //this.set_proxy(null);
        }

        private string AuthToken
        {
            get
            {
                lock (_authTokenLock)
                {
                    return _authToken;
                }
            }
            set
            {
                lock (_authTokenLock)
                {
                    _authToken = value;
                }
            }
        }

        private string PartnerId
        {
            get
            {
                lock (_partnerIdLock)
                {
                    return _partnerId;
                }
            }
            set
            {
                lock (_partnerIdLock)
                {
                    _partnerId = value;
                }
            }
        }

        private string UserId
        {
            get
            {
                lock (_userIdLock)
                {
                    return _userId;
                }
            }
            set
            {
                lock (_userIdLock)
                {
                    _userId = value;
                }
            }
        }

        public System.Collections.Generic.List<Station> Stations { get; private set; }

        public string ImageCachePath { get; set; } = "";

        [System.ComponentModel.DefaultValue(true)]
        public bool HasSubscription { get; private set; }

        public string AudioFormat
        {
            get { return _audioFormat; }
            set { SetAudioFormat(value); }
        }

        public bool ForceSsl { get; set; } = false;

        public SortOrder StationSortOrder { get; set; }

        #region SortOrder enum

        public enum SortOrder
        {
            DateAsc,
            DateDesc,
            AlphaAsc,
            AlphaDesc,
            RatingAsc,
            RatingDesc
        }

        #endregion

        private readonly object _authTokenLock = new object();
        private readonly object _partnerIdLock = new object();
        private readonly object _rpcCountLock = new object();
        private readonly object _userIdLock = new object();
        private string _audioFormat = PAudioFormat.Mp3;

        private bool _authorizing;

        private string _authToken;
        private bool _connected;
        //private bool _firstAuthComplete;

        private string _partnerId;
        private string _password = "";
        private int _rpcCount;
        private long _syncTime;
        private long _timeSynced;

        private string _user = "";
        private string _userId;
        //private string _listenerId;

        protected internal System.Collections.Generic.List<string> QuickMixStationIDs;

        public event ConnectionEventHandler ConnectionEvent;
        public event StationsUpdatedEventHandler StationUpdateEvent;
        public event StationsUpdatingEventHandler StationsUpdatingEvent;
        public event FeedbackUpdateEventHandler FeedbackUpdateEvent;
        public event LoginStatusEventHandler LoginStatusEvent;
        public event QuickMixSavedEventHandler QuickMixSavedEvent;

        protected internal string RpcRequest(string url, string data)
        {
            try
            {
                return Util.PRequest.StringRequest(url, data);
            }
            catch (System.Exception e)
            {
                Util.Log.O(e.ToString());
                throw new PandoraException(Util.ErrorCodes.ErrorRpc, e);
            }
        }

        //Checks for fault returns.  If it's an Auth fault (auth timed out)
        //return false, which signals that a re-auth and retry needs to be done
        //otherwise return true signalling all clear.
        //All other faults will be thrown
        protected internal bool HandleFaults(JsonResult result, bool secondTry)
        {
            if (!result.Fault) return true; //no fault
            if (result.FaultCode == Util.ErrorCodes.InvalidAuthToken)
                if (!secondTry)
                    return false; //auth fault, signal a re-auth

            Util.Log.O("Fault: " + result.FaultString);
            throw new PandoraException(result.FaultCode); //other, throw the exception
        }

        protected internal string CallRPC_Internal(string method, Newtonsoft.Json.Linq.JObject request, bool isAuth,
            bool useSsl = false)
        {
            int callId;
            lock (_rpcCountLock)
            {
                callId = _rpcCount++;
            }

            string url = (useSsl || ForceSsl ? "https://" : "http://") + Const.RpcUrl + "?method=" + method;

            if (request == null) request = new Newtonsoft.Json.Linq.JObject();

            if (AuthToken != null && PartnerId != null)
            {
                //if (!url.EndsWith("?")) url += "?";
                url += "&partner_id=" + PartnerId;
                url += "&auth_token=" + System.Uri.EscapeDataString(AuthToken);

                if (UserId != null)
                {
                    url += "&user_id=" + UserId;
                    request["userAuthToken"] = AuthToken;
                    request["syncTime"] = AdjustedSyncTime();
                }
            }

            string json = request.ToString();
            string data = method == "auth.partnerLogin" ? json : Crypto.OutKey.Encrypt(json);

            Util.Log.O("[" + callId + ":url]: " + url);

            if (isAuth)
                Util.Log.O("[" + callId + ":json]: " +
                           Util.StringExtensions.SanitizeJson(json)
                               .Replace(_password, "********")
                               .Replace(_user, "********"));
            else
                Util.Log.O("[" + callId + ":json]: " + Util.StringExtensions.SanitizeJson(json));

            //if reauthorizing, wait until it completes.
            if (!isAuth)
            {
                int waitCount = 30;
                while (_authorizing)
                {
                    waitCount--;
                    if (waitCount >= 0)
                        System.Threading.Thread.Sleep(1000);
                    else
                        break;
                }
            }

            string response = RpcRequest(url, data);
            Util.Log.O("[" + callId + ":response]: " + Util.StringExtensions.SanitizeJson(response));
            return response;
        }

        protected internal JsonResult CallRPC(string method, Newtonsoft.Json.Linq.JObject request = null,
            bool isAuth = false, bool useSsl = false)
        {
            string response = CallRPC_Internal(method, request, isAuth, useSsl);
            JsonResult result = new JsonResult(response);
            if (!result.Fault) return result;
            if (HandleFaults(result, false)) return result;
            Util.Log.O("Reauth Required");
            if (!AuthenticateUser())
            {
                HandleFaults(result, true);
            }
            else
            {
                CallRPC_Internal(method, request, isAuth, useSsl);
                HandleFaults(result, true);
            }

            return result;
        }

        protected internal JsonResult CallRPC(string method, params object[] args)
        {
            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject();
            if (args.Length%2 != 0)
            {
                Util.Log.O("CallRPC: Called with an uneven number of arguments!");
                return null;
            }

            for (int i = 0; i < args.Length; i += 2)
            {
                if (args[i].GetType() != typeof (string) || args[i].GetType() != typeof (string))
                {
                    Util.Log.O("CallRPC: Called with an incorrect parameter type!");
                    return null;
                }
                req[(string) args[i]] = Newtonsoft.Json.Linq.JToken.FromObject(args[i + 1]);
            }

            return CallRPC(method, req);
        }

        protected internal object CallRPC(string method, object[] args, bool bUrlArgs = false, bool isAuth = false,
            bool useSsl = false, bool insertTime = true)
        {
            return null;
        }

        public void RefreshStations()
        {
            Util.Log.O("RefreshStations");
            StationsUpdatingEvent?.Invoke(this);

            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["includeStationArtUrl"] = true};
            JsonResult stationList = CallRPC("user.getStationList", req);

            QuickMixStationIDs.Clear();

            Stations = new System.Collections.Generic.List<Station>();
            Newtonsoft.Json.Linq.JToken stations = stationList.Result["stations"];
            foreach (Newtonsoft.Json.Linq.JToken d in stations)
            {
                Stations.Add(new Station(this, d));
            }
            //foreach (PDict s in stationList)
            //    Stations.Add(new Station(this, s));

            if (QuickMixStationIDs.Count > 0)
            {
                foreach (Station s in Stations)
                {
                    if (QuickMixStationIDs.Contains(s.Id))
                        s.UseQuickMix = true;
                }
            }

            System.Collections.Generic.List<Station> quickMixes = Stations.FindAll(x => x.IsQuickMix);
            Stations = Stations.FindAll(x => !x.IsQuickMix);

            switch (StationSortOrder)
            {
                case SortOrder.DateDesc:
                    //Stations = Stations.OrderByDescending(x => x.ID).ToList();
                    Stations =
                        Stations.OrderByDescending(x => System.Convert.ToInt64(x.Id)).ToList();
                    break;
                case SortOrder.DateAsc:
                    //Stations = Stations.OrderBy(x => x.ID).ToList();
                    Stations = Stations.OrderBy(x => System.Convert.ToInt64(x.Id)).ToList();
                    break;
                case SortOrder.AlphaDesc:
                    Stations = Stations.OrderByDescending(x => x.Name).ToList();
                    break;
                case SortOrder.AlphaAsc:
                    Stations = Stations.OrderBy(x => x.Name).ToList();
                    break;
                case SortOrder.RatingAsc:
                    GetStationMetaData();
                    Stations = Stations.OrderBy(x => x.ThumbsUp).ToList();
                    break;
                case SortOrder.RatingDesc:
                    GetStationMetaData();
                    Stations = Stations.OrderByDescending(x => x.ThumbsUp).ToList();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            Stations.InsertRange(0, quickMixes);

            StationUpdateEvent?.Invoke(this);
        }

        //private string getSyncKey()
        //{
        //    string result = string.Empty;

        //    try
        //    {
        //        var keyArray = new Util.Downloader().DownloadString(Const.SYNC_KEY_URL);

        //        var vals = keyArray.Split('|');
        //        if (vals.Length < 3) return result;
        //        var len = 48;
        //        if (!Int32.TryParse(vals[1], out len)) return result;
        //        if (vals[2].Length != len) return result;

        //        Log.O("Sync Key Age (sec): " + vals[0]);
        //        Log.O("Sync Key Length: " + vals[1]);
        //        Log.O("Sync Key: " + vals[2]);

        //        result = vals[2];
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.O(ex.ToString());
        //    }

        //    return result;
        //}

        //private string getSyncTime()
        //{
        //    string result = string.Empty;

        //    try
        //    {
        //        result = new Util.Downloader().DownloadString(Const.SYNC_TIME_URL);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.O(ex.ToString());
        //    }

        //    return result;
        //}

        public void Logout()
        {
            //_firstAuthComplete = false;
        }

        public long AdjustedSyncTime()
        {
            return _syncTime + (Time.Unix() - _timeSynced);
        }

        public bool AuthenticateUser()
        {
            _authorizing = true;

            Util.Log.O("AuthUser");

            //_listenerId = null;
            //webAuthToken = null;
            AuthToken = null;
            PartnerId = null;
            UserId = null;

            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["username"] = "android", ["password"] = "AC7IBG09A3DTSYM4R41UJWL07VLN8JI7", ["deviceModel"] = "android-generic", ["version"] = "5", ["includeUrls"] = true};

            JsonResult ret;

            try
            {
                ret = CallRPC("auth.partnerLogin", req, true, true);
                if (ret.Fault)
                {
                    Util.Log.O("PartnerLogin Error: " + ret.FaultString);
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Util.Log.O(e.ToString());
                return false;
            }

            Newtonsoft.Json.Linq.JToken result = ret["result"];

            _syncTime = Crypto.DecryptSyncTime(result["syncTime"].ToString());
            _timeSynced = Time.Unix();

            PartnerId = result["partnerId"].ToString();
            AuthToken = result["partnerAuthToken"].ToString();

            req = new Newtonsoft.Json.Linq.JObject {["loginType"] = "user", ["username"] = _user, ["password"] = _password, ["includePandoraOneInfo"] = true, ["includeAdAttributes"] = true, ["includeSubscriptionExpiration"] = true, ["partnerAuthToken"] = AuthToken, ["syncTime"] = _syncTime};

            //req["includeStationArtUrl"] = true;
            //req["returnStationList"] = true;

            // AdjustedSyncTime();

            ret = CallRPC("auth.userLogin", req, true, true);
            if (ret.Fault)
            {
                Util.Log.O("UserLogin Error: " + ret.FaultString);
                return false;
            }

            result = ret["result"];
            AuthToken = result["userAuthToken"].ToString();
            UserId = result["userId"].ToString();
            HasSubscription = !result["hasAudioAds"].ToObject<bool>();

            _authorizing = false;
            return true;
        }

        private void SendLoginStatus(string status)
        {
            LoginStatusEvent?.Invoke(this, status);
        }

        public void Connect(string user, string password)
        {
            Util.Log.O("Connect");
            Util.ErrorCodes status = Util.ErrorCodes.Success;
            _connected = false;

            _user = user;
            _password = password;

            try
            {
                SendLoginStatus("Authenticating user:\r\n" + user);
                _connected = AuthenticateUser();

                if (_connected)
                {
                    SendLoginStatus("Loading station list...");
                    RefreshStations();
                }
                else
                {
                    status = Util.ErrorCodes.ErrorRpc;
                }
            }
            catch (PandoraException ex)
            {
                status = ex.Fault;
                _connected = false;
            }
            catch (System.Exception ex)
            {
                status = Util.ErrorCodes.UnknownError;
                Util.Log.O("Connection Error: " + ex);
                _connected = false;
            }

            ConnectionEvent?.Invoke(this, _connected, status);
        }

        //public void SetProxy()
        //{

        //}

        public void SetAudioFormat(string fmt)
        {
            if ((fmt != PAudioFormat.AacPlus && fmt != PAudioFormat.Mp3 && fmt != PAudioFormat.Mp3Hifi) || (!HasSubscription && fmt == PAudioFormat.Mp3Hifi))
            {
                fmt = PAudioFormat.Mp3;
            }

            _audioFormat = fmt;
        }

        public void SaveQuickMix()
        {
            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["quickMixStationIds"] = new Newtonsoft.Json.Linq.JArray((from s in Stations where s.UseQuickMix select s.Id).ToArray().Cast<string>())};

            CallRPC("user.setQuickMix", req);

            QuickMixSavedEvent?.Invoke(this);
        }

        public System.Collections.Generic.List<SearchResult> Search(string query)
        {
            Util.Log.O("Search: " + query);
            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["searchText"] = query};
            JsonResult search = CallRPC("music.search", req);

            Newtonsoft.Json.Linq.JToken artists = search.Result["artists"];
            Newtonsoft.Json.Linq.JToken songs = search.Result["songs"];
            System.Collections.Generic.List<SearchResult> list = artists.Select(a => new SearchResult(SearchResultType.Artist, a)).ToList();
            list.AddRange(songs.Select(s => new SearchResult(SearchResultType.Song, s)));

            list = list.OrderByDescending(i => i.Score).ToList();

            return list;
        }

        public Station CreateStationFromSearch(string token)
        {
            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["musicToken"] = token};
            JsonResult result = CallRPC("station.createStation", req);

            Station station = new Station(this, result.Result);
            Stations.Add(station);

            return station;
        }

        private Station CreateStation(Song song, string type)
        {
            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["trackToken"] = song.TrackToken, ["musicType"] = type};
            JsonResult result = CallRPC("station.createStation", req);

            Station station = new Station(this, result.Result);
            Stations.Add(station);

            return station;
        }

        private void GetStationMetaData()
        {
            Util.Log.O("RetrieveStationMetaData");

            System.Threading.Tasks.Parallel.ForEach(Stations, station =>
            {
                Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["stationToken"] = station.IdToken, ["includeExtendedAttributes"] = true};

                JsonResult stationInfo = CallRPC("station.getStation", req);

                Newtonsoft.Json.Linq.JToken feedback = stationInfo.Result["feedback"];

                station.ThumbsUp = System.Convert.ToInt32(feedback["totalThumbsUp"].ToString());
                station.ThumbsDown = System.Convert.ToInt32(feedback["totalThumbsDown"].ToString());
            });
        }

        public Station CreateStationFromSong(Song song)
        {
            return CreateStation(song, "song");
        }

        public Station CreateStationFromArtist(Song song)
        {
            return CreateStation(song, "artist");
        }

        public void AddFeedback(string stationToken, string trackToken, SongRating rating)
        {
            Util.Log.O("AddFeedback");

            bool rate = rating == SongRating.Love;

            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["stationToken"] = stationToken, ["trackToken"] = trackToken, ["isPositive"] = rate};

            CallRPC("station.addFeedback", req);
        }

        public void DeleteFeedback(string feedbackId)
        {
            Util.Log.O("DeleteFeedback");

            CallRPC("station.deleteFeedback", "feedbackId", feedbackId);
        }

        public void CallFeedbackUpdateEvent(Song song, bool success)
        {
            FeedbackUpdateEvent?.Invoke(this, song, success);
        }

        public Station GetStationById(string id)
        {
            return Stations.FirstOrDefault(s => s.Id == id);
        }

        public string GetFeedbackId(string stationToken, string trackToken)
        {
            Newtonsoft.Json.Linq.JObject req = new Newtonsoft.Json.Linq.JObject {["stationToken"] = stationToken, ["trackToken"] = trackToken, ["isPositive"] = true};

            JsonResult feedback = CallRPC("station.addFeedback", req);
            return (string) feedback.Result["feedbackId"];
        }

        #region Delegates

        public delegate void ConnectionEventHandler(object sender, bool state, Util.ErrorCodes code);

        public delegate void FeedbackUpdateEventHandler(object sender, Song song, bool success);

        public delegate void LoginStatusEventHandler(object sender, string status);

        public delegate void PandoraErrorEventHandler(object sender, string errorCode, string msg);

        public delegate void StationsUpdatedEventHandler(object sender);

        public delegate void StationsUpdatingEventHandler(object sender);

        public delegate void QuickMixSavedEventHandler(object sender);

        #endregion
    }
}