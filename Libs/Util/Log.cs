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
    public class Log
    {
        #region Delegates

        public delegate void LogMessageEventHandler(string msg);

        #endregion

        private static string _logPath;
        private static System.IO.StreamWriter _sw;
        private static readonly object SwLock = new object();

        public static bool WriteTimestamp { get; set; } = true;

        public static event LogMessageEventHandler LogMessage;

        public static void SetLogPath(string path, bool append = false)
        {
            _logPath = path;

            lock (SwLock)
            {
                if (_sw != null)
                {
                    _sw.Flush();
                    _sw.Close();
                }

                try
                {
                    _sw = new System.IO.StreamWriter(_logPath, append);
                }
                catch (System.Exception)
                {
                    _sw = null;
                    throw;
                }
            }
        }

        public static void O(string msg, params object[] arg)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(msg, arg);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine(msg);
            }

            if (_sw == null) return;

            lock (SwLock)
            {
                string timestamp = "";
                if (WriteTimestamp) timestamp = "[" + System.DateTime.Now.ToString("HH:mm:ss.fff") + "] ";

                try
                {
                    _sw.WriteLine(timestamp + msg, arg);
                    _sw.Flush();
                }
                catch (System.FormatException)
                {
                    try
                    {
                        _sw.WriteLine(timestamp + msg);
                        _sw.Flush();
                    }
                    catch
                    {
                        //todo
                    }
                }
                catch
                {
                    //todo
                }
            }

            OnLog(string.Format(msg, arg));
        }

        private static void OnLog(string msg)
        {
            LogMessage?.Invoke(msg);
        }
    }
}