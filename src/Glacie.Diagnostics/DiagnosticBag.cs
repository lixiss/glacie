using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Glacie.Diagnostics
{
    /// <summary>
    /// Represents a mutable bag of diagnostics. You can add diagnostics to the
    /// bag, and also get all the diagnostics out of the bag.  Once added,
    /// diagnostics cannot be removed, and no ordering is guaranteed.
    /// </summary>
    public sealed class DiagnosticBag : IReadOnlyCollection<Diagnostic>
    {
        private ConcurrentQueue<Diagnostic>? _lazyItems;

        public int Count => _lazyItems?.Count ?? 0;

        public void Add(Diagnostic value)
        {
            GetItems().Enqueue(value);
        }

        public void AddRange(IEnumerable<Diagnostic> values)
        {
            var items = GetItems();
            foreach (var value in values)
            {
                items.Enqueue(value);
            }
        }

        public IEnumerator<Diagnostic> GetEnumerator()
        {
            if (_lazyItems == null)
            {
                return Enumerable.Empty<Diagnostic>().GetEnumerator();
            }
            return _lazyItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ImmutableArray<Diagnostic> ToReadOnly()
        {
            if (_lazyItems == null || _lazyItems.Count == 0)
                return ImmutableArray<Diagnostic>.Empty;

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(_lazyItems);
            return diagnostics.MoveToImmutable();
        }


        private ConcurrentQueue<Diagnostic> GetItems()
        {
            var bag = _lazyItems;
            if (bag != null) return bag;

            bag = new ConcurrentQueue<Diagnostic>();
            return Interlocked.CompareExchange(ref _lazyItems, bag, null) ?? bag;
        }
    }
}
