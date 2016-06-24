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

using Enumerable = System.Linq.Enumerable;

namespace Elpis.Util
{
    public static class DateTimeExtensions
    {
        public static int ToEpochTime(this System.DateTime time)
        {
            System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            return (int) time.Subtract(epoch).TotalSeconds;
        }
    }

    public static class StringExtensions
    {
        public static string SanitizeJson(this string json)
        {
            return json.Replace("{", "<").Replace("}", ">");
        }

        public static string Md5Hash(this string data)
        {
            return HashHelper.GetStringHash(data);
        }

        public static string ToHex(this System.Collections.Generic.IEnumerable<int> str)
        {
            return Enumerable.Aggregate(str, "", (current, tmp) => current + $"{System.Convert.ToUInt32(tmp.ToString()):x2}");
        }

        public static string FromHex(this string hex)
        {
            if (hex.Length%2 != 0)
                throw new System.ArgumentException("Input must be hex values and have an even number of characters.");

            string result = string.Empty;
            for (int i = 0; i < hex.Length; i += 2)
            {
                result += (char) System.Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return result;
        }

        public static string SafeSubstring(this string str, int startIndex, int length)
        {
            if (startIndex + length > str.Length - 1)
                length = str.Length - 1 - startIndex;

            return str.Substring(startIndex, length);
        }

        public static string SafeSubstring(this string str, int length)
        {
            return str.SafeSubstring(0, length);
        }

        public static string StringEllipses(this string str, int maxlen)
        {
            string sub = str.SafeSubstring(maxlen);
            if (sub.Length < str.Length)
            {
                sub = str.SafeSubstring(maxlen - 3) + "...";
            }

            return sub;
        }
    }
}