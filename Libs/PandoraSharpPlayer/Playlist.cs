/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of PandoraSharpPlayer.
 * PandoraSharpPlayer is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * PandoraSharpPlayer is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with PandoraSharpPlayer. If not, see http://www.gnu.org/licenses/.
*/

using Elpis.PandoraSharp;

namespace Elpis.PandoraSharpPlayer
{
    public class Playlist
    {
        public Playlist(int maxPlayed = 4, int lowCount = 1)
        {
            MaxPlayed = maxPlayed;
            LowPlaylistCount = lowCount;
            _nextSongs = new System.Collections.Concurrent.ConcurrentQueue<Song>();
            _playedSongs = new System.Collections.Concurrent.ConcurrentQueue<Song>();
        }

        public int MaxPlayed { get; set; }
        public int LowPlaylistCount { get; set; }

        public Song Current
        {
            get
            {
                lock (_currentLock)
                {
                    return _currentSong;
                }
            }

            set
            {
                lock (_currentLock)
                {
                    _currentSong = value;
                }
            }
        }

        private readonly object _currentLock = new object();

        private readonly System.Collections.Concurrent.ConcurrentQueue<Song> _nextSongs;
        private readonly System.Collections.Concurrent.ConcurrentQueue<Song> _playedSongs;
        private Song _currentSong;

        private bool _emptyPlaylist;

        public event PlaylistLowHandler PlaylistLow;
        public event CurrentSongChangedHandler CurrentSongChanged;
        public event PlayedSongDequeuedHandler PlayedSongDequeued;
        public event PlayedSongQueuedHandler PlayedSongQueued;

        public void ClearSongs()
        {
            Song trash;
            while (_nextSongs.TryDequeue(out trash)) {}
        }

        public void ClearHistory()
        {
            Song trash;
            while (_playedSongs.TryDequeue(out trash)) {}
        }

        public int AddSongs(System.Collections.Generic.List<Song> songs)
        {
            if (songs.Count == 0)
            {
                _emptyPlaylist = true;
                return 0;
            }

            foreach (Song s in songs)
            {
                Util.Log.O("Adding: " + s);
                _nextSongs.Enqueue(s);
            }

            return songs.Count;
        }

        private void WaitForPlaylistReload()
        {
            System.DateTime start = System.DateTime.Now;
            Util.Log.O("Waiting for playlist to reload.");
            while (_nextSongs.IsEmpty && !_emptyPlaylist)
            {
                if ((System.DateTime.Now - start).TotalSeconds >= 60)
                {
                    Util.Log.O("Playlist did not reload within 60 seconds, ");
                    throw new PandoraException(Util.ErrorCodes.EndOfPlaylist);
                }

                System.Threading.Thread.Sleep(25);
            }

            if (_emptyPlaylist)
            {
                Util.Log.O("WaitForPlaylist: Still Empty");
                throw new PandoraException(Util.ErrorCodes.EndOfPlaylist);
            }

            Util.Log.O("WaitForPlaylist: Complete");
        }

        private Song DequeueSong()
        {
            Song result;
            if (!_nextSongs.TryDequeue(out result))
                throw new PandoraException(Util.ErrorCodes.EndOfPlaylist);

            return result;
        }

        private void SendPlaylistLow()
        {
            _emptyPlaylist = false;
            PlaylistLow?.Invoke(this, _nextSongs.Count);
        }

        public void DoReload()
        {
            ClearSongs();
            SendPlaylistLow();

            try
            {
                WaitForPlaylistReload();
            }
            catch
            {
                if (_nextSongs.IsEmpty)
                    throw;
            }
        }

        public Song NextSong()
        {
            if (_nextSongs.IsEmpty)
            {
                Util.Log.O("PlaylistEmpty - Reloading");

                DoReload();
            }

            Song next = DequeueSong();

            if (!next.IsStillValid)
            {
                Util.Log.O("Song was invalid, reloading and skipping any more invalid songs.");
                //clear songs that are now invalid
                DoReload();
            }

            Util.Log.O("NextSong: " + next);
            Song oldSong = Current;

            Current = next;

            if (_nextSongs.Count <= LowPlaylistCount)
            {
                Util.Log.O("PlaylistLow");
                SendPlaylistLow();
            }

            CurrentSongChanged?.Invoke(this, Current);

            if (oldSong != null && oldSong.Played)
            {
                _playedSongs.Enqueue(oldSong);
                Util.Log.O("SongQueued");
                PlayedSongQueued?.Invoke(this, oldSong);
            }

            if (_playedSongs.Count <= MaxPlayed) return Current;
            Song trash;
            if (!_playedSongs.TryDequeue(out trash)) return Current;
            Util.Log.O("OldSongDequeued");
            PlayedSongDequeued?.Invoke(this, trash);

            return Current;
        }

        #region Nested type: PlaylistEmptyException

        public class PlaylistEmptyException : System.Exception {}

        #endregion

        #region Delegates

        public delegate void CurrentSongChangedHandler(object sender, Song newSong);

        public delegate void PlayedSongDequeuedHandler(object sender, Song oldSong);

        public delegate void PlayedSongQueuedHandler(object sender, Song newSong);

        public delegate void PlaylistLowHandler(object sender, int count);

        #endregion
    }
}