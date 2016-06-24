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

namespace PandoraSharp
{
    public enum SearchResultType
    {
        Artist,
        Song
    }

    public class SearchResult
    {
        public SearchResult(SearchResultType type, Newtonsoft.Json.Linq.JToken d)
        {
            ResultType = type;
            Score = (int) d["score"];
            MusicToken = (string) d["musicToken"];

            switch (ResultType) {
                case SearchResultType.Song:
                    Title = (string) d["songName"];
                    Artist = (string) d["artistName"];
                    break;
                case SearchResultType.Artist:
                    Name = (string) d["artistName"];
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        public SearchResultType ResultType { get; }
        public int Score { get; private set; }
        public string MusicToken { get; private set; }
        public string Title { get; }
        public string Artist { get; }
        public string Name { get; }

        public string DisplayName
        {
            get
            {
                if (ResultType == SearchResultType.Artist)
                    return Name; // + ": " + this.Score.ToString();
                return Title + " by " + Artist; // +": " + this.Score.ToString(); ;
            }
        }
    }
}