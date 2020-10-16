using System;
using System.Collections.Generic;
using System.Text;

namespace Glacie.Collections
{
    // See https://github.com/dotnet/runtime/issues/27229

    // There is stub implementation, which doesn't act nicely, but better than
    // doing without it.

    internal sealed class StringHashSet
    {
        private readonly HashSet<string> _items;

        public StringHashSet()
        {
            _items = new HashSet<string>();
        }

        public StringHashSet(IEnumerable<string> collection)
        {
            _items = new HashSet<string>(collection);
        }

        public StringHashSet(IEqualityComparer<string>? comparer)
        {
            _items = new HashSet<string>(comparer);
        }

        public StringHashSet(int capacity)
        {
            _items = new HashSet<string>(capacity);
        }

        public StringHashSet(IEnumerable<string> collection, IEqualityComparer<string>? comparer)
        {
            _items = new HashSet<string>(collection, comparer);
        }

        public StringHashSet(int capacity, IEqualityComparer<string>? comparer)
        {
            _items = new HashSet<string>(capacity, comparer);
        }

        // protected HashSet(SerializationInfo info, StreamingContext context);

    }
}
