using System;
using System.Collections;
using System.Collections.Generic;

namespace Glacie.Data.Arz.Infrastructure
{
    // TODO: (Low) (ArzRecordClassTable) use separate type, like arz_record_type_id.
    // TODO: (Low) (ArzRecordClassTable) Ideally we need string table which can lookup by String and Span<char> and Span<byte>. Something based on XHashTable may be done.
    // TODO: (Medium) (ArzRecordClassTable) Now 0 index means what class is not defined. Consider to use empty string instead. Presense of class can be tracked in ArzRecord.

    /// <summary>
    /// Two-way map, which map string onto index, and index onto string.
    /// It is similar to ArzStringTable, but used exclusively for record types.
    /// This also has special mapping between 0 and null value.
    /// </summary>
    public sealed class ArzRecordClassTable
    {
        private readonly List<string> _indexToValueMap;
        private readonly Dictionary<string, int> _valueToIndexMap;

        public ArzRecordClassTable()
        {
            _indexToValueMap = new List<string>();
            _valueToIndexMap = new Dictionary<string, int>(StringComparer.Ordinal);

            // Record type table reserves 0 index to unassigned value (and it
            // may be guessed from `Class` field). There is no mapping between
            // string representation of this, so just put null here.
            _indexToValueMap.Add(null!);
        }

        #region API

        public int Count => _indexToValueMap.Count;

        public string? this[int index] => GetString(index);

        public int this[string value] => GetIndex(value);

        public int GetOrAdd(string? value)
        {
            if (value == null) return 0;

            if (_valueToIndexMap.TryGetValue(value, out var index))
            {
                return index;
            }
            else
            {
                return AddCore(value);
            }
        }

        public ValueCollection GetValues()
        {
            return new ValueCollection(this);
        }

        public readonly struct ValueCollection : IReadOnlyCollection<string>
        {
            private readonly ArzRecordClassTable _collection;

            internal ValueCollection(ArzRecordClassTable collection)
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

        private string GetString(int index)
        {
            return _indexToValueMap[index];
        }

        private int GetIndex(string value)
        {
            if (value == null) return 0;
            return _valueToIndexMap[value];
        }

        private int AddCore(string value)
        {
            var newIndex = _indexToValueMap.Count;
            _valueToIndexMap.Add(value, newIndex);
            _indexToValueMap.Add(value);
            return newIndex;
        }
    }
}
