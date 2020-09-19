using System;
using System.Collections.Generic;

namespace Glacie.Collections
{
    public sealed class StringInterner : IStringInterner
    {
        private readonly HashSet<string> _items;

        public StringInterner()
        {
            _items = new HashSet<string>();
        }

        public StringInterner(StringComparer comparer)
        {
            _items = new HashSet<string>(comparer);
        }

        public int Count => _items.Count;

        public string Intern(string value)
        {
            if (_items.TryGetValue(value, out var result))
            {
                return result;
            }

            if (_items.Add(value))
            {
                return value;
            }
            else throw Error.Unreachable();
        }

        public string Intern(ReadOnlySpan<char> value)
        {
            // TODO: (Low) (StringInterner) Need StringKeyed collections with ReadOnlySpan<char> lookups.
            return Intern(value.ToString());
        }
    }
}
