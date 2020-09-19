using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Glacie.Diagnostics
{
    /// <summary>
    /// Represents a mutable bag of diagnostics. You can add diagnostics to the bag,
    /// and also get all the diagnostics out of the bag (the bag implements
    /// IEnumerable&lt;Diagnostics&gt;. Once added, diagnostics cannot be removed, and no ordering
    /// is guaranteed.
    /// </summary>
    public sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private ConcurrentQueue<Diagnostic>? _lazyItems;

        public int Count => _lazyItems?.Count ?? 0;

        public void Add(Diagnostic value)
        {
            GetItems().Enqueue(value);
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

        private ConcurrentQueue<Diagnostic> GetItems()
        {
            var bag = _lazyItems;
            if (bag != null) return bag;

            bag = new ConcurrentQueue<Diagnostic>();
            return Interlocked.CompareExchange(ref _lazyItems, bag, null) ?? bag;
        }
    }
}
