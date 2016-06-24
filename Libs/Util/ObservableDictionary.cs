/* Copyright (c) 2007, Dr. WPF
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *   * Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 * 
 *   * Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 * 
 *   * The name Dr. WPF may not be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY Dr. WPF ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL Dr. WPF BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Util
{
    [System.Serializable]
    public class ObservableDictionary<TKey, TValue> : System.Collections.Generic.IDictionary<TKey, TValue>,
        System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>,
        System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>,
        System.Collections.IDictionary, System.Collections.ICollection, System.Collections.IEnumerable,
        System.Runtime.Serialization.ISerializable, System.Runtime.Serialization.IDeserializationCallback,
        System.Collections.Specialized.INotifyCollectionChanged, System.ComponentModel.INotifyPropertyChanged
    {
        #region protected classes

        #region KeyedDictionaryEntryCollection<TKey>

#pragma warning disable 693
        protected class KeyedDictionaryEntryCollection<TKey> :
#pragma warning restore 693
            System.Collections.ObjectModel.KeyedCollection<TKey, System.Collections.DictionaryEntry>
        {
            #region methods

            #region protected

            protected override TKey GetKeyForItem(System.Collections.DictionaryEntry entry)
            {
                return (TKey) entry.Key;
            }

            #endregion protected

            #endregion methods

            #region constructors

            #region public

            public KeyedDictionaryEntryCollection() {}

            public KeyedDictionaryEntryCollection(System.Collections.Generic.IEqualityComparer<TKey> comparer)
                : base(comparer) {}

            #endregion public

            #endregion constructors
        }

        #endregion KeyedDictionaryEntryCollection<TKey>

        #endregion protected classes

        #region public structures

        #region Enumerator

        [System.Serializable,
         System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
#pragma warning disable 693
        public struct Enumerator<TKey, TValue> :
#pragma warning restore 693
            System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>,
            System.IDisposable, System.Collections.IDictionaryEnumerator, System.Collections.IEnumerator
        {
            #region constructors

            internal Enumerator(ObservableDictionary<TKey, TValue> dictionary, bool isDictionaryEntryEnumerator)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = -1;
                _isDictionaryEntryEnumerator = isDictionaryEntryEnumerator;
                _current = new System.Collections.Generic.KeyValuePair<TKey, TValue>();
            }

            #endregion constructors

            #region properties

            #region public

            public System.Collections.Generic.KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    ValidateCurrent();
                    return _current;
                }
            }

            #endregion public

            #endregion properties

            #region methods

            #region public

            public void Dispose() {}

            public bool MoveNext()
            {
                ValidateVersion();
                _index++;
                if (_index < _dictionary.KeyedEntryCollection.Count)
                {
                    _current =
                        new System.Collections.Generic.KeyValuePair<TKey, TValue>(
                            (TKey) _dictionary.KeyedEntryCollection[_index].Key,
                            (TValue) _dictionary.KeyedEntryCollection[_index].Value);
                    return true;
                }
                _index = -2;
                _current = new System.Collections.Generic.KeyValuePair<TKey, TValue>();
                return false;
            }

            #endregion public

            #region private

            private void ValidateCurrent()
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (_index) {
                    case -1:
                        throw new System.InvalidOperationException("The enumerator has not been started.");
                    case -2:
                        throw new System.InvalidOperationException("The enumerator has reached the end of the collection.");
                }
            }

            private void ValidateVersion()
            {
                if (_version != _dictionary._version)
                {
                    throw new System.InvalidOperationException(
                        "The enumerator is not valid because the dictionary changed.");
                }
            }

            #endregion private

            #endregion methods

            #region IEnumerator implementation

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    ValidateCurrent();
                    if (_isDictionaryEntryEnumerator)
                    {
                        return new System.Collections.DictionaryEntry(_current.Key, _current.Value);
                    }
                    return new System.Collections.Generic.KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                ValidateVersion();
                _index = -1;
                _current = new System.Collections.Generic.KeyValuePair<TKey, TValue>();
            }

            #endregion IEnumerator implemenation

            #region IDictionaryEnumerator implemenation

            System.Collections.DictionaryEntry System.Collections.IDictionaryEnumerator.Entry
            {
                get
                {
                    ValidateCurrent();
                    return new System.Collections.DictionaryEntry(_current.Key, _current.Value);
                }
            }

            object System.Collections.IDictionaryEnumerator.Key
            {
                get
                {
                    ValidateCurrent();
                    return _current.Key;
                }
            }

            object System.Collections.IDictionaryEnumerator.Value
            {
                get
                {
                    ValidateCurrent();
                    return _current.Value;
                }
            }

            #endregion

            #region fields

            private readonly ObservableDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private System.Collections.Generic.KeyValuePair<TKey, TValue> _current;
            private readonly bool _isDictionaryEntryEnumerator;

            #endregion fields
        }

        #endregion Enumerator

        #endregion public structures

        #region constructors

        #region public

        public ObservableDictionary()
        {
            KeyedEntryCollection = new KeyedDictionaryEntryCollection<TKey>();
        }

        public ObservableDictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary)
        {
            KeyedEntryCollection = new KeyedDictionaryEntryCollection<TKey>();

            foreach (System.Collections.Generic.KeyValuePair<TKey, TValue> entry in dictionary)
                DoAddEntry(entry.Key, entry.Value);
        }

        public ObservableDictionary(System.Collections.Generic.IEqualityComparer<TKey> comparer)
        {
            KeyedEntryCollection = new KeyedDictionaryEntryCollection<TKey>(comparer);
        }

        public ObservableDictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary,
            System.Collections.Generic.IEqualityComparer<TKey> comparer)
        {
            KeyedEntryCollection = new KeyedDictionaryEntryCollection<TKey>(comparer);

            foreach (System.Collections.Generic.KeyValuePair<TKey, TValue> entry in dictionary)
                DoAddEntry(entry.Key, entry.Value);
        }

        #endregion public

        #region protected

        protected ObservableDictionary(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            _siInfo = info;
        }

        #endregion protected

        #endregion constructors

        #region properties

        #region public

        public System.Collections.Generic.IEqualityComparer<TKey> Comparer => KeyedEntryCollection.Comparer;

        public int Count => KeyedEntryCollection.Count;

        public System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection Keys => TrueDictionary.Keys;

        public TValue this[TKey key]
        {
            get { return (TValue) KeyedEntryCollection[key].Value; }
            set { DoSetEntry(key, value); }
        }

        public System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection Values => TrueDictionary.Values;

        #endregion public

        #region private

        private System.Collections.Generic.Dictionary<TKey, TValue> TrueDictionary
        {
            get
            {
                if (_dictionaryCacheVersion == _version) return _dictionaryCache;
                _dictionaryCache.Clear();
                foreach (System.Collections.DictionaryEntry entry in KeyedEntryCollection)
                    _dictionaryCache.Add((TKey) entry.Key, (TValue) entry.Value);
                _dictionaryCacheVersion = _version;
                return _dictionaryCache;
            }
        }

        #endregion private

        #endregion properties

        #region methods

        #region public

        public void Add(TKey key, TValue value)
        {
            DoAddEntry(key, value);
        }

        public void Clear()
        {
            DoClearEntries();
        }

        public bool ContainsKey(TKey key)
        {
            return KeyedEntryCollection.Contains(key);
        }

        public bool ContainsValue(TValue value)
        {
            return TrueDictionary.ContainsValue(value);
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return new Enumerator<TKey, TValue>(this, false);
        }

        public bool Remove(TKey key)
        {
            return DoRemoveEntry(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            bool result = KeyedEntryCollection.Contains(key);
            value = result ? (TValue) KeyedEntryCollection[key].Value : default(TValue);
            return result;
        }

        #endregion public

        #region protected

        protected virtual bool AddEntry(TKey key, TValue value)
        {
            KeyedEntryCollection.Add(new System.Collections.DictionaryEntry(key, value));
            return true;
        }

        protected virtual bool ClearEntries()
        {
            // check whether there are entries to clear
            bool result = Count > 0;
            if (result)
            {
                // if so, clear the dictionary
                KeyedEntryCollection.Clear();
            }
            return result;
        }

        protected int GetIndexAndEntryForKey(TKey key, out System.Collections.DictionaryEntry entry)
        {
            entry = new System.Collections.DictionaryEntry();
            int index = -1;
            if (!KeyedEntryCollection.Contains(key)) return index;
            entry = KeyedEntryCollection[key];
            index = KeyedEntryCollection.IndexOf(entry);
            return index;
        }

        protected virtual void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }

        protected virtual bool RemoveEntry(TKey key)
        {
            // remove the entry
            return KeyedEntryCollection.Remove(key);
        }

        protected virtual bool SetEntry(TKey key, TValue value)
        {
            bool keyExists = KeyedEntryCollection.Contains(key);

            // if identical key/value pair already exists, nothing to do
            if (keyExists && value.Equals((TValue) KeyedEntryCollection[key].Value))
                return false;

            // otherwise, remove the existing entry
            if (keyExists)
                KeyedEntryCollection.Remove(key);

            // add the new entry
            KeyedEntryCollection.Add(new System.Collections.DictionaryEntry(key, value));

            return true;
        }

        #endregion protected

        #region private

        private void DoAddEntry(TKey key, TValue value)
        {
            if (!AddEntry(key, value)) return;
            _version++;

            System.Collections.DictionaryEntry entry;
            int index = GetIndexAndEntryForKey(key, out entry);
            FireEntryAddedNotifications(entry, index);
        }

        private void DoClearEntries()
        {
            if (!ClearEntries()) return;
            _version++;
            FireResetNotifications();
        }

        private bool DoRemoveEntry(TKey key)
        {
            System.Collections.DictionaryEntry entry;
            int index = GetIndexAndEntryForKey(key, out entry);

            bool result = RemoveEntry(key);
            if (!result) return false;
            _version++;
            if (index > -1)
                FireEntryRemovedNotifications(entry, index);

            return true;
        }

        private void DoSetEntry(TKey key, TValue value)
        {
            System.Collections.DictionaryEntry entry;
            int index = GetIndexAndEntryForKey(key, out entry);

            if (!SetEntry(key, value)) return;
            _version++;

            // if prior entry existed for this key, fire the removed notifications
            if (index > -1)
            {
                FireEntryRemovedNotifications(entry, index);

                // force the property change notifications to fire for the modified entry
                _countCache--;
            }

            // then fire the added notifications
            index = GetIndexAndEntryForKey(key, out entry);
            FireEntryAddedNotifications(entry, index);
        }

        private void FireEntryAddedNotifications(System.Collections.DictionaryEntry entry, int index)
        {
            // fire the relevant PropertyChanged notifications
            FirePropertyChangedNotifications();

            // fire CollectionChanged notification
            if (index > -1)
                OnCollectionChanged(
                    new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                        System.Collections.Specialized.NotifyCollectionChangedAction.Add,
                        new System.Collections.Generic.KeyValuePair<TKey, TValue>((TKey) entry.Key, (TValue) entry.Value),
                        index));
            else
                OnCollectionChanged(
                    new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                        System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        private void FireEntryRemovedNotifications(System.Collections.DictionaryEntry entry, int index)
        {
            // fire the relevant PropertyChanged notifications
            FirePropertyChangedNotifications();

            // fire CollectionChanged notification
            if (index > -1)
                OnCollectionChanged(
                    new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                        System.Collections.Specialized.NotifyCollectionChangedAction.Remove,
                        new System.Collections.Generic.KeyValuePair<TKey, TValue>((TKey) entry.Key, (TValue) entry.Value),
                        index));
            else
                OnCollectionChanged(
                    new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                        System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        private void FirePropertyChangedNotifications()
        {
            if (Count == _countCache) return;
            _countCache = Count;
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnPropertyChanged("Keys");
            OnPropertyChanged("Values");
        }

        private void FireResetNotifications()
        {
            // fire the relevant PropertyChanged notifications
            FirePropertyChangedNotifications();

            // fire CollectionChanged notification
            OnCollectionChanged(
                new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        #endregion private

        #endregion methods

        #region interfaces

        #region IDictionary<TKey, TValue>

        void System.Collections.Generic.IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            DoAddEntry(key, value);
        }

        bool System.Collections.Generic.IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return DoRemoveEntry(key);
        }

        bool System.Collections.Generic.IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return KeyedEntryCollection.Contains(key);
        }

        bool System.Collections.Generic.IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return TryGetValue(key, out value);
        }

        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey, TValue>.Keys => Keys;

        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey, TValue>.Values => Values;

        TValue System.Collections.Generic.IDictionary<TKey, TValue>.this[TKey key]
        {
            get { return (TValue) KeyedEntryCollection[key].Value; }
            set { DoSetEntry(key, value); }
        }

        #endregion IDictionary<TKey, TValue>

        #region IDictionary

        void System.Collections.IDictionary.Add(object key, object value)
        {
            DoAddEntry((TKey) key, (TValue) value);
        }

        void System.Collections.IDictionary.Clear()
        {
            DoClearEntries();
        }

        bool System.Collections.IDictionary.Contains(object key)
        {
            return KeyedEntryCollection.Contains((TKey) key);
        }

        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator()
        {
            return new Enumerator<TKey, TValue>(this, true);
        }

        bool System.Collections.IDictionary.IsFixedSize => false;

        bool System.Collections.IDictionary.IsReadOnly => false;

        object System.Collections.IDictionary.this[object key]
        {
            get { return KeyedEntryCollection[(TKey) key].Value; }
            set { DoSetEntry((TKey) key, (TValue) value); }
        }

        System.Collections.ICollection System.Collections.IDictionary.Keys => Keys;

        void System.Collections.IDictionary.Remove(object key)
        {
            DoRemoveEntry((TKey) key);
        }

        System.Collections.ICollection System.Collections.IDictionary.Values => Values;

        #endregion IDictionary

        #region ICollection<KeyValuePair<TKey, TValue>>

        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Add(
            System.Collections.Generic.KeyValuePair<TKey, TValue> kvp)
        {
            DoAddEntry(kvp.Key, kvp.Value);
        }

        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Clear()
        {
            DoClearEntries();
        }

        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Contains(
            System.Collections.Generic.KeyValuePair<TKey, TValue> kvp)
        {
            return KeyedEntryCollection.Contains(kvp.Key);
        }

        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.CopyTo(
            System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
            {
                // ReSharper disable once NotResolvedInText
                throw new System.ArgumentNullException(@"CopyTo() failed:  array parameter was null");
            }
            if ((index < 0) || (index > array.Length))
            {
                throw new System.ArgumentOutOfRangeException(
                    // ReSharper disable once NotResolvedInText
                    "CopyTo() failed:  index parameter was outside the bounds of the supplied array");
            }
            if (array.Length - index < KeyedEntryCollection.Count)
            {
                throw new System.ArgumentException("CopyTo() failed:  supplied array was too small");
            }

            foreach (System.Collections.DictionaryEntry entry in KeyedEntryCollection)
                array[index++] = new System.Collections.Generic.KeyValuePair<TKey, TValue>((TKey) entry.Key,
                    (TValue) entry.Value);
        }

        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Count => KeyedEntryCollection.Count;

        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Remove(
            System.Collections.Generic.KeyValuePair<TKey, TValue> kvp)
        {
            return DoRemoveEntry(kvp.Key);
        }

        #endregion ICollection<KeyValuePair<TKey, TValue>>

        #region ICollection

        void System.Collections.ICollection.CopyTo(System.Array array, int index)
        {
            ((System.Collections.ICollection) KeyedEntryCollection).CopyTo(array, index);
        }

        int System.Collections.ICollection.Count => KeyedEntryCollection.Count;

        bool System.Collections.ICollection.IsSynchronized => ((System.Collections.ICollection) KeyedEntryCollection).IsSynchronized;

        object System.Collections.ICollection.SyncRoot => ((System.Collections.ICollection) KeyedEntryCollection).SyncRoot;

        #endregion ICollection

        #region IEnumerable<KeyValuePair<TKey, TValue>>

        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>
            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator<TKey, TValue>(this, false);
        }

        #endregion IEnumerable<KeyValuePair<TKey, TValue>>

        #region IEnumerable

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable

        #region ISerializable

        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            if (info == null)
            {
                throw new System.ArgumentNullException(nameof(info));
            }

            System.Collections.ObjectModel.Collection<System.Collections.DictionaryEntry> entries =
                new System.Collections.ObjectModel.Collection<System.Collections.DictionaryEntry>();
            foreach (System.Collections.DictionaryEntry entry in KeyedEntryCollection)
                entries.Add(entry);
            info.AddValue("entries", entries);
        }

        #endregion ISerializable

        #region IDeserializationCallback

        public virtual void OnDeserialization(object sender)
        {
            if (_siInfo == null) return;
            System.Collections.ObjectModel.Collection<System.Collections.DictionaryEntry> entries =
                (System.Collections.ObjectModel.Collection<System.Collections.DictionaryEntry>)
                    _siInfo.GetValue("entries",
                        typeof (System.Collections.ObjectModel.Collection<System.Collections.DictionaryEntry>));
            foreach (System.Collections.DictionaryEntry entry in entries)
                AddEntry((TKey) entry.Key, (TValue) entry.Value);
        }

        #endregion IDeserializationCallback

        #region INotifyCollectionChanged

        event System.Collections.Specialized.NotifyCollectionChangedEventHandler System.Collections.Specialized.
            INotifyCollectionChanged.CollectionChanged
        {
            add { CollectionChanged += value; }
            remove { CollectionChanged -= value; }
        }

        protected virtual event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion INotifyCollectionChanged

        #region INotifyPropertyChanged

        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.
            PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged

        #endregion interfaces

        #region fields

        protected KeyedDictionaryEntryCollection<TKey> KeyedEntryCollection;

        private int _countCache;

        private readonly System.Collections.Generic.Dictionary<TKey, TValue> _dictionaryCache =
            new System.Collections.Generic.Dictionary<TKey, TValue>();

        private int _dictionaryCacheVersion;
        private int _version;

        [System.NonSerialized] private readonly System.Runtime.Serialization.SerializationInfo _siInfo;

        #endregion fields
    }
}