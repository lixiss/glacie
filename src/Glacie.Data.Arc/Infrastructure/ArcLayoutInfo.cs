namespace Glacie.Data.Arc.Infrastructure
{
    public sealed class ArcLayoutInfo
    {
        public int EntryCount { get; internal set; }

        public int RemovedEntryCount { get; internal set; }

        public int ChunkCount { get; internal set; }

        public int LiveChunkCount { get; internal set; }

        /// <summary>
        /// When this value more than zero, then chunked entries
        /// doesn't store chunks linearly. To linearize them you need rebuild
        /// whole file (recompress for example). Note that nor Compact nor
        /// Repack doesn't change chunk order.
        /// </summary>
        public int UnorderedChunkCount { get; internal set; }

        public int FreeSegmentCount { get; internal set; }

        public long FreeSegmentBytes { get; internal set; }

        public bool CanCompact => RemovedEntryCount > 0
            || LiveChunkCount < ChunkCount
            || FreeSegmentCount > 0
            || FreeSegmentBytes > 0;

        public bool CanDefragment => UnorderedChunkCount > 0;
    }
}
