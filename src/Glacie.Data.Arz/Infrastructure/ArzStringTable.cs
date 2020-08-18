using System;
using System.Collections;
using System.Collections.Generic;

namespace Glacie.Data.Arz.Infrastructure
{
    // TODO: (Low) (ArzStringTable) might be renamed and moved to core library. String tables appears in other files too.

    /// <summary>
    /// Two-way map, which map string value onto index, and index onto string.
    /// Index is zero-based. When new string added, index automatically generated.
    /// </summary>
    public sealed class ArzStringTable
    {
        private static StringComparer GetStringComparer() => StringComparer.Ordinal;

        private readonly List<string> _indexToValueMap;
        private readonly Dictionary<string, arz_string_id> _valueToIndexMap;

        #region Contruction

        public ArzStringTable()
        {
            _indexToValueMap = new List<string>();
            _valueToIndexMap = new Dictionary<string, arz_string_id>(GetStringComparer());
        }

        public ArzStringTable(int capacity)
        {
            _indexToValueMap = new List<string>(capacity);
            _valueToIndexMap = new Dictionary<string, arz_string_id>(capacity, GetStringComparer());
        }

        public ArzStringTable(List<string> values, bool takeOwnership = false)
        {
            _indexToValueMap = takeOwnership ? values : new List<string>(values.Count);
            _valueToIndexMap = new Dictionary<string, arz_string_id>(values.Count, GetStringComparer());

            if (takeOwnership)
            {
                var count = values.Count;
                for (var i = 0; i < count; i++)
                {
                    _valueToIndexMap.Add(values[i], (arz_string_id)i);
                }
            }
            else
            {
                foreach (var value in values)
                {
                    AddCore(value);
                }
            }
        }

        public ArzStringTable(string[] values)
        {
            _indexToValueMap = new List<string>(values.Length);
            _valueToIndexMap = new Dictionary<string, arz_string_id>(values.Length, GetStringComparer());

            foreach (var value in values)
            {
                AddCore(value);
            }
        }

        public ArzStringTable(IEnumerable<string> values)
        {
            _indexToValueMap = new List<string>();
            _valueToIndexMap = new Dictionary<string, arz_string_id>(GetStringComparer());

            foreach (var value in values)
            {
                AddCore(value);
            }
        }

        #endregion

        #region API

        public int Count => _indexToValueMap.Count;

        public string this[arz_string_id index] => GetString(index);

        public arz_string_id this[string value] => GetIndex(value);

        public bool TryGet(string value, out arz_string_id index)
        {
            return _valueToIndexMap.TryGetValue(value, out index);
        }

        public arz_string_id GetOrAdd(string value)
        {
            if (_valueToIndexMap.TryGetValue(value, out var index))
            {
                return index;
            }
            else
            {
                return AddCore(value);
            }
        }

        public arz_string_id Add(string value) => AddCore(value);

        public ValueCollection GetValues()
        {
            return new ValueCollection(this);
        }

        public readonly struct ValueCollection : IReadOnlyCollection<string>
        {
            private readonly ArzStringTable _collection;

            internal ValueCollection(ArzStringTable collection)
            {
                _collection = collection;
            }

            public List<string>.Enumerator GetEnumerator()
            {
                return _collection._indexToValueMap.GetEnumerator();
            }

            public int Count => _collection.Count;

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        private string GetString(arz_string_id index)
        {
            return _indexToValueMap[(int)index];
        }

        private arz_string_id GetIndex(string value)
        {
            return _valueToIndexMap[value];
        }

        private arz_string_id AddCore(string value)
        {
            var newIndex = (arz_string_id)_indexToValueMap.Count;
            _valueToIndexMap.Add(value, newIndex);
            _indexToValueMap.Add(value);
            return newIndex;
        }
    }
}
