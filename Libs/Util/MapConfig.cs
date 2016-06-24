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

using System.Linq;

namespace Elpis.Util
{
    public class MapConfigEntry
    {
        public MapConfigEntry(string key, object defaultValue)
        {
            //Type t = def.GetType();
            //if (t != typeof (string) &&
            //    t != typeof (String) &&
            //    t != typeof (int) &&
            //    t != typeof (double) &&
            //    t != typeof (bool))
            //{
            //    throw new Exception("Type must be string, int, double or bool");
            //}

            Key = key;
            Default = defaultValue;
        }

        public string Key { get; }
        public object Default { get; }
    }

    public class MapConfig
    {
        public MapConfig(string configPath = "")
        {
            ConfigPath = configPath;
            AutoSave = false;
            AutoSetDefaults = true;

            _map = new System.Collections.Generic.Dictionary<string, object>();
        }

        private const string CryptCheck = "*_a3fc756b42_*";
        public string LastConfig { get; private set; } = string.Empty;

        public string ConfigPath { get; }

        public bool AutoSave { get; set; }

        public bool AutoSetDefaults { get; set; }
        private readonly string _cryptPass = SystemInfo.GetUniqueHash();
        private readonly System.Collections.Generic.Dictionary<string, object> _map;

        public bool LoadConfig(string configData = "")
        {
            System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();

            bool result = true;
            System.IO.TextReader tr = null;
            try
            {
                if (configData == "")
                {
                    if (ConfigPath == "") return false;

                    if (!System.IO.File.Exists(ConfigPath))
                    {
                        System.IO.File.Create(ConfigPath).Close();
                        return true;
                    }

                    tr = new System.IO.StreamReader(ConfigPath, System.Text.Encoding.Unicode);
                }
                else
                {
                    tr = new System.IO.StringReader(configData);
                }
                string line = "";
                while ((line = tr.ReadLine()) != null)
                    lines.Add(line);

                LastConfig = string.Empty;
                foreach (string l in lines)
                    LastConfig += l + "\r\n";
            }
            catch (System.Exception ex)
            {
                Log.O(ex.ToString());
                result = false;
            }
            finally
            {
                tr?.Close();
            }

            if (!result) return false;
            {
                foreach (string line in lines)
                {
                    try
                    {
                        string[] split = line.Split('|');

                        if (split.Length != 2) continue;
                        //Deal with our lists of config items saved out to keys that look like Key[ID]
                        if (split[0].Contains("["))
                        {
                            string[] splitList = split[0].Split(new[] {"[", "]"}, System.StringSplitOptions.None);
                            if (!_map.ContainsKey(splitList[0]))
                            {
                                _map[splitList[0]] = new System.Collections.Generic.Dictionary<int, string>();
                            }
                            ((System.Collections.Generic.Dictionary<int, string>) _map[splitList[0]])[
                                int.Parse(splitList[1])] = split[1];
                        }
                        //This is the normal stype of config entry - just a string
                        else
                        {
                            if (_map.ContainsKey(split[0]))
                                _map[split[0]] = split[1]; //cascading style. newer values override old
                            else
                                _map.Add(split[0], split[1]);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.O(ex.ToString());
                    }
                }
            }

            return true;
        }

        public bool SaveConfig()
        {
            System.Collections.Generic.List<string> configs = new System.Collections.Generic.List<string>();

            foreach (System.Collections.Generic.KeyValuePair<string, object> kvp in _map)
            {
                System.Type t = kvp.Value.GetType();
                if (t.IsGenericType)
                {
                    configs.AddRange(from item in (System.Collections.Generic.Dictionary<int, string>) kvp.Value select kvp.Key + '[' + item.Key + ']' + '|' + item.Value);
                }
                else
                {
                    configs.Add(kvp.Key + '|' + kvp.Value);
                }
            }

            bool result = true;
            System.IO.StreamWriter sw = null;
            try
            {
                sw = new System.IO.StreamWriter(ConfigPath, false, System.Text.Encoding.Unicode);
                LastConfig = string.Empty;
                foreach (string line in configs.ToArray())
                {
                    sw.WriteLine(line);
                    LastConfig += line + "\r\n";
                }
            }
            catch (System.Exception ex)
            {
                Log.O(ex.ToString());
                result = false;
            }
            finally
            {
                sw?.Close();
            }

            return result;
        }

        public void SetValue(string key, System.Collections.Generic.Dictionary<int, string> value)
        {
            if (_map.ContainsKey(key))
                _map[key] = value;
            else
                _map.Add(key, value);
        }

        public void SetValue(string key, string value)
        {
            value = value.Replace(@"|", @"&sep;");
            if (_map.ContainsKey(key))
                _map[key] = value;
            else
                _map.Add(key, value);

            if (AutoSave)
                SaveConfig();
        }

        public void SetValue(string key, int value)
        {
            SetValue(key, value.ToString());
        }

        public void SetValue(string key, double value)
        {
            SetValue(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public void SetValue(string key, bool value)
        {
            SetValue(key, value.ToString());
        }

        public void SetValue(MapConfigEntry entry, object value)
        {
            System.Type t = value.GetType();
            System.Type dType = entry.Default.GetType();
            if (t != dType && !(t.IsGenericType && t.GetGenericTypeDefinition() == dType.GetGenericTypeDefinition()))
                throw new System.Exception("Value type does not equal default value type.");

            if (t == typeof (string) || t == typeof (string)) SetValue(entry.Key, (string) value);
            else if (t == typeof (int)) SetValue(entry.Key, (int) value);
            else if (t == typeof (double)) SetValue(entry.Key, (double) value);
            else if (t == typeof (bool) || t == typeof (bool)) SetValue(entry.Key, (bool) value);
            else if (t == typeof (System.Collections.Generic.Dictionary<int, string>))
                SetValue(entry.Key, (System.Collections.Generic.Dictionary<int, string>) value);
            else SetValue(entry.Key, value.ToString());
        }

        public void SetEncryptedString(MapConfigEntry entry, string value)
        {
            System.Type t = entry.Default.GetType();
            if (!(t == typeof (string) || t == typeof (string)))
                throw new System.Exception("SetEncryptedString only works on string entries.");

            SetValue(entry, StringCrypt.EncryptString(CryptCheck + value, _cryptPass));
        }

        public string GetValue(string key, string defValue)
        {
            System.Diagnostics.Debug.Assert(key != null, "key != null");    
            if (!_map.ContainsKey(key) || (string) _map[key] == null)
            {
                if (AutoSetDefaults)
                    SetValue(key, defValue);
                return defValue;
            }

            return ((string) _map[key]).Replace(@"&sep;", @"|");
        }

        public int GetValue(string key, int defValue)
        {
            string value = GetValue(key, defValue.ToString());
            int result;
            int.TryParse(value, out result);
            return result;
        }

        public double GetValue(string key, double defValue)
        {
            string value = GetValue(key, defValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            // ReSharper disable once RedundantAssignment
            double result = defValue;
            double.TryParse(value, out result);
            return result;
        }

        public bool GetValue(string key, bool defValue)
        {
            string value = GetValue(key, defValue.ToString());
            // ReSharper disable once RedundantAssignment
            bool result = defValue;
            bool.TryParse(value, out result);
            return result;
        }

        private System.Collections.Generic.Dictionary<int, string> GetValue(string key,
            System.Collections.Generic.Dictionary<int, string> defValue)
        {
            if (_map.ContainsKey(key))
                return (System.Collections.Generic.Dictionary<int, string>) _map[key];

            if (AutoSetDefaults)
                SetValue(key, defValue);

            return defValue;
        }

        public object GetValue(MapConfigEntry entry)
        {
            System.Type t = entry.Default.GetType();

            if (t == typeof (string) || t == typeof (string)) return GetValue(entry.Key, (string) entry.Default);
            if (t == typeof (int)) return GetValue(entry.Key, (int) entry.Default);
            if (t == typeof (double)) return GetValue(entry.Key, (double) entry.Default);
            if (t == typeof (bool) || t == typeof (bool)) return GetValue(entry.Key, (bool) entry.Default);
            if (t == typeof (System.Collections.Generic.Dictionary<int, string>))
                return GetValue(entry.Key, (System.Collections.Generic.Dictionary<int, string>) entry.Default);
            return GetValue(entry.Key, entry.Default.ToString()); //On their own to parse it
        }

        public string GetEncryptedString(MapConfigEntry entry)
        {
            System.Type t = entry.Default.GetType();
            if (!(t == typeof (string) || t == typeof (string)))
                throw new System.Exception("GetEncryptedString only works on string entries.");

            string result;
            try
            {
                result = StringCrypt.DecryptString((string) GetValue(entry), _cryptPass);
                if (result != string.Empty)
                {
                    result = result.StartsWith(CryptCheck) ? result.Replace(CryptCheck, string.Empty) : string.Empty;
                }
            }
            catch
            {
                result = string.Empty;
            }

            return result;
        }
    }
}