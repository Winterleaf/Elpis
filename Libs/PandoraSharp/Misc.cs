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
    public enum SongRating
    {
        Ban = 0,
        Love = 1,
        None = 2
    }

    public static class PAudioFormat
    {
        //AAC+, 64Kbps
        public static readonly string AacPlus = "aacplus";
        //MP3, VBR 128Kbps
        public static readonly string Mp3 = "mp3";
        //MP3, CBR 192Kbps - Pandora One users only.
        //Will default back to MP3 if selected without a One account. (enforced on server)
        public static readonly string Mp3Hifi = "mp3-hifi";
    }

    internal class Const
    {
        public static readonly string RpcUrl = @"tuner.pandora.com/services/json/";
        public static readonly int HttpTimeout = 30;
        public static readonly string AudioFormat = PAudioFormat.AacPlus;

        public static readonly int PlaylistValidityTime = (int) (60*60*0.5);

        public static readonly string CryptInKey = @"R=U!LH$O2B#";
        public static readonly string CryptOutKey = @"6#26FRL$ZWD";
    }

    public class PDict : System.Collections.Generic.Dictionary<string, object> {}
}