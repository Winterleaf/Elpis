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

namespace Elpis
{
    public class HotKeyEventArgs : System.EventArgs
    {
        public HotKeyEventArgs(HotKey hotKey)
        {
            HotKey = hotKey;
        }

        public HotKey HotKey { get; private set; }
    }

    public class HotKeyAlreadyRegisteredException : System.Exception
    {
        public HotKeyAlreadyRegisteredException(string message, HotKey hotKey) : base(message)
        {
            HotKey = hotKey;
        }

        public HotKeyAlreadyRegisteredException(string message, HotKey hotKey, System.Exception inner)
            : base(message, inner)
        {
            HotKey = hotKey;
        }

        public HotKey HotKey { get; private set; }
    }

    public class HotKeyNotSupportedException : System.Exception
    {
        public HotKeyNotSupportedException(string message, HotKey hotKey) : base(message)
        {
            HotKey = hotKey;
        }

        public HotKeyNotSupportedException(string message, HotKey hotKey, System.Exception inner) : base(message, inner)
        {
            HotKey = hotKey;
        }

        public HotKey HotKey { get; private set; }
    }

    public class HotKey : System.ComponentModel.INotifyPropertyChanged, System.IEquatable<HotKey>
    {
        protected HotKey() {}

        public HotKey(System.Windows.Input.RoutedUICommand command, System.Windows.Input.Key key,
            System.Windows.Input.ModifierKeys modifiers, bool global, bool enabled = true)
        {
            Key = key;
            Modifiers = modifiers;
            Enabled = enabled;
            Global = global;
            Command = command;
        }

        public HotKey(System.Windows.Input.RoutedUICommand command, System.Windows.Input.Key key,
            System.Windows.Input.ModifierKeys modifiers) : this(command, key, modifiers, false) {}

        public System.Windows.Input.RoutedUICommand Command
        {
            get { return _command; }
            set
            {
                if (_command == value) return;
                _command = value;
                OnPropertyChanged("Command");
            }
        }

        public System.Windows.Input.Key Key
        {
            get { return _key; }
            set
            {
                if (_key == value) return;
                OnPropertyChanging("Key");
                _key = value;
                OnPropertyChanged("Key");
            }
        }

        public System.Windows.Input.ModifierKeys Modifiers
        {
            get { return _modifiers; }
            set
            {
                if (_modifiers == value) return;
                OnPropertyChanging("Modifiers");
                _modifiers = value;
                OnPropertyChanged("Modifiers");
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value == _enabled) return;
                _enabled = value;
                OnPropertyChanged("Enabled");
            }
        }

        public bool Global
        {
            get { return _global; }
            set
            {
                if (value == _global) return;
                OnPropertyChanging("Global");
                _global = value;
                OnPropertyChanged("Global");
            }
        }

        public string KeysString => (Modifiers == System.Windows.Input.ModifierKeys.None ? "" : Modifiers + " + ") + Key;

        private System.Windows.Input.RoutedUICommand _command;

        private bool _enabled;

        private bool _global;

        private System.Windows.Input.Key _key;

        private System.Windows.Input.ModifierKeys _modifiers;

        public bool Equals(HotKey other)
        {
            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void SetKeyCombo(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers)
        {
            OnPropertyChanging("Key");
            _key = key;
            _modifiers = modifiers;
            OnPropertyChanged("Key");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        public virtual void OnPropertyChanging(string propertyName)
        {
            PropertyChanging?.Invoke(this, new System.ComponentModel.PropertyChangingEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            HotKey hotKey = obj as HotKey;
            return hotKey != null && Equals(hotKey);
        }

        public override int GetHashCode()
        {
            return (int) Modifiers + 10*(int) Key;
        }

        public override string ToString()
        {
            return $"{Key} + {Modifiers} ({(Enabled ? "" : "Not ")}Enabled), {(Global ? "Global" : "")}";
        }
    }

    public static class PlayerCommands
    {
        public static System.Windows.Input.RoutedUICommand PlayPause =
            new System.Windows.Input.RoutedUICommand("Pause currently playing track or Play if paused", "Play/Pause",
                typeof (PlayerCommands));

        public static System.Windows.Input.RoutedUICommand Next =
            new System.Windows.Input.RoutedUICommand("Skips currently playing track", "Skip Song",
                typeof (PlayerCommands));

        public static System.Windows.Input.RoutedUICommand ThumbsUp =
            new System.Windows.Input.RoutedUICommand("Marks this as a liked track that suits this station", "Thumbs Up",
                typeof (PlayerCommands));

        public static System.Windows.Input.RoutedUICommand ThumbsDown =
            new System.Windows.Input.RoutedUICommand(
                "Marks this as a disliked track or one that doesn't suit this station", "Thumbs Down",
                typeof (PlayerCommands));

        public static System.Collections.Generic.List<System.Windows.Input.RoutedUICommand> AllCommands => new System.Collections.Generic.List<System.Windows.Input.RoutedUICommand>
        {
            PlayPause,
            Next,
            ThumbsUp,
            ThumbsDown
        };

        public static System.Windows.Input.RoutedUICommand GetCommandByName(string name)
        {
            if (name == PlayPause.Name) return PlayPause;
            if (name == Next.Name) return Next;
            if (name == ThumbsUp.Name) return ThumbsUp;
            return name == ThumbsDown.Name ? ThumbsDown : null;
        }
    }

    public sealed class HotKeyHost : System.IDisposable
    {
        public HotKeyHost(System.Windows.Window window)
        {
            _window = window;
            System.Windows.Interop.HwndSource hwnd =
                (System.Windows.Interop.HwndSource) System.Windows.PresentationSource.FromVisual(window);
            Init(hwnd);
        }

        private static readonly SerialCounter IdGen = new SerialCounter(-1);
        public bool IsEnabled { get; set; }

        public Util.ObservableDictionary<int, HotKey> HotKeys { get; } =
            new Util.ObservableDictionary<int, HotKey>();

        private readonly System.Windows.Window _window;

        private void Init(System.Windows.Interop.HwndSource hhwndSource)
        {
            if (hhwndSource == null)
                throw new System.ArgumentNullException(nameof(hhwndSource));

            _hook = WndProc;
            _hwndSource = hhwndSource;
            hhwndSource.AddHook(_hook);

            IsEnabled = true;
        }

        private void RegisterHotKey(int id, HotKey hotKey)
        {
            if (hotKey.Global)
            {
                RegisterGlobalHotKey(id, hotKey);
            }
            else
            {
                RegisterActiveWindowHotkey(hotKey);
            }
        }

        private void UnregisterHotKey(int id)
        {
            HotKey hotKey = HotKeys[id];
            if (hotKey.Global)
            {
                UnregisterGlobalHotKey(id);
            }
            else
            {
                UnregisterActiveWindowHotkey(hotKey);
            }
        }

        private System.IntPtr WndProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam,
            ref bool handled)
        {
            if (msg != WmHotKey) return new System.IntPtr(0);
            Util.Log.O("HotKeys WndProc: IsEnabled - {0}", IsEnabled.ToString());
            if (!IsEnabled || !HotKeys.ContainsKey((int) wParam)) return new System.IntPtr(0);
            HotKey h = HotKeys[(int) wParam];
            Util.Log.O("HotKeys WndProc: HotKey - {0}", h.KeysString);
            if (!h.Global) return new System.IntPtr(0);
            h.Command?.Execute(null, _window);

            return new System.IntPtr(0);
        }

        private void hotKey_PropertyChanging(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.PropertyName)
            {
                case "Global":
                case "Modifiers":
                case "Key":
                    System.Collections.Generic.KeyValuePair<int, HotKey> kvPair = Enumerable.FirstOrDefault(HotKeys,
                        h => Equals(h.Value, sender));
                    if (kvPair.Value != null)
                    {
                        UnregisterHotKey(kvPair.Key);
                    }
                    break;
            }
        }

        private void hotKey_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            System.Collections.Generic.KeyValuePair<int, HotKey> kvPair = Enumerable.FirstOrDefault(HotKeys,
                h => Equals(h.Value, sender));
            if (kvPair.Value == null) return;
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.PropertyName)
            {
                case "Enabled":
                    if (kvPair.Value.Enabled)
                        RegisterHotKey(kvPair.Key, kvPair.Value);
                    else
                        UnregisterHotKey(kvPair.Key);
                    break;
                case "Global":
                case "Modifiers":
                case "Key":
                    if (kvPair.Value.Enabled)
                    {
                        RegisterHotKey(kvPair.Key, kvPair.Value);
                    }
                    break;
            }
        }

        public HotKey AddHotKey(HotKey hotKey)
        {
            try
            {
                if (hotKey == null)
                    throw new System.ArgumentNullException(nameof(hotKey));
                /* We let em add as many null keys to the list as they want, but never register them*/
                if (hotKey.Key != System.Windows.Input.Key.None && HotKeys.ContainsValue(hotKey))
                {
                    throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);
                    //Log.O("HotKey already registered!");
                }

                try
                {
                    int id = IdGen.Next();
                    if (hotKey.Enabled && hotKey.Key != System.Windows.Input.Key.None)
                    {
                        RegisterHotKey(id, hotKey);
                    }
                    hotKey.PropertyChanging += hotKey_PropertyChanging;
                    hotKey.PropertyChanged += hotKey_PropertyChanged;
                    HotKeys[id] = hotKey;
                    return hotKey;
                }
                catch (HotKeyNotSupportedException)
                {
                    return null;
                }
            }
            catch (HotKeyAlreadyRegisteredException)
            {
                Util.Log.O("HotKey already registered!");
            }
            return null;
        }

        public bool RemoveHotKey(HotKey hotKey)
        {
            System.Collections.Generic.KeyValuePair<int, HotKey> kvPair = Enumerable.FirstOrDefault(HotKeys,
                h => Equals(h.Value, hotKey));
            if (kvPair.Value != null)
            {
                kvPair.Value.PropertyChanged -= hotKey_PropertyChanged;
                if (kvPair.Value.Enabled)
                    UnregisterHotKey(kvPair.Key);
                return HotKeys.Remove(kvPair.Key);
            }
            return false;
        }

        public class SerialCounter
        {
            public SerialCounter(int start)
            {
                Current = start;
            }

            public int Current { get; private set; }

            public int Next()
            {
                return ++Current;
            }
        }

        #region HotKey Interop

        private const int WmHotKey = 786;

        [System.Runtime.InteropServices.DllImport("user32", CharSet = System.Runtime.InteropServices.CharSet.Ansi,
            SetLastError = true, ExactSpelling = true)]
        private static extern int RegisterHotKey(System.IntPtr hwnd, int id, int modifiers, int key);

        [System.Runtime.InteropServices.DllImport("user32", CharSet = System.Runtime.InteropServices.CharSet.Ansi,
            SetLastError = true, ExactSpelling = true)]
        private static extern int UnregisterHotKey(System.IntPtr hwnd, int id);

        #endregion

        #region Interop-Encapsulation

        private System.Windows.Interop.HwndSourceHook _hook;
        private System.Windows.Interop.HwndSource _hwndSource;

        private void RegisterGlobalHotKey(int id, HotKey hotKey)
        {
            if ((int) _hwndSource.Handle != 0)
            {
                RegisterHotKey(_hwndSource.Handle, id, (int) hotKey.Modifiers,
                    System.Windows.Input.KeyInterop.VirtualKeyFromKey(hotKey.Key));
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (error == 0) return;
                System.Exception e = new System.ComponentModel.Win32Exception(error);

                if (error == 1409)
                    throw new HotKeyAlreadyRegisteredException(e.Message, hotKey, e);
                throw e;
            }
            throw new System.InvalidOperationException("Handle is invalid");
        }

        private void UnregisterGlobalHotKey(int id)
        {
            if ((int) _hwndSource.Handle == 0) return;
            UnregisterHotKey(_hwndSource.Handle, id);
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            if (error != 0)
                throw new System.ComponentModel.Win32Exception(error);
        }

        #endregion

        #region ActiveWindowHotkeyBinding

        private static void RegisterActiveWindowHotkey(HotKey hotkey)
        {
            try
            {
                hotkey.Command.InputGestures.Add(new System.Windows.Input.KeyGesture(hotkey.Key, hotkey.Modifiers));
            }
            catch (System.NotSupportedException e)
            {
                throw new HotKeyNotSupportedException(
                    "Alphanumeric Keys without modifiers are not supported as hotkeys", hotkey, e);
            }
        }

        private void UnregisterActiveWindowHotkey(HotKey hotkey)
        {
            foreach (System.Windows.Input.KeyGesture keygesture in Enumerable.Where(Enumerable.Cast<System.Windows.Input.KeyGesture>(hotkey.Command.InputGestures), keygesture => keygesture.Key == hotkey.Key && keygesture.Modifiers == hotkey.Modifiers)) {
                hotkey.Command.InputGestures.Remove(keygesture);
                break;
            }
        }

        #endregion

        #region Destructor

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _hwndSource.RemoveHook(_hook);
            }

            for (int i = HotKeys.Count - 1; i >= 0; i--)
            {
                UnregisterGlobalHotKey(i);
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        ~HotKeyHost()
        {
            Dispose(false);
        }

        #endregion
    }
}