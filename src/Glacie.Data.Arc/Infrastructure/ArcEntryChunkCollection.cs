using System;

namespace Glacie.Data.Arc.Infrastructure
{
    public readonly struct ArcEntryChunkCollection
    {
        private readonly Memory<ArcEntryChunk> _items;

        internal ArcEntryChunkCollection(Memory<ArcEntryChunk> items)
        {
            _items = items;
        }

        public int Count => _items.Length;

        public readonly ref readonly ArcEntryChunk this[int index]
        {
            get
            {
                return ref _items.Span[index];
            }
        }
    }
}
