namespace Glacie.Data.Arc.Infrastructure
{
    internal struct AllocatedSegment
    {
        public long Offset;
        public long Length;

        public arc_entry_id EntryId;

        public bool IsChunk;
        public int ChunkIndex;

        public AllocatedSegment(long offset, long length, arc_entry_id entryId)
        {
            Offset = offset;
            Length = length;
            EntryId = entryId;
            IsChunk = false;
            ChunkIndex = -1;
        }

        public AllocatedSegment(long offset, long length, arc_entry_id entryId, int chunkIndex)
        {
            Offset = offset;
            Length = length;
            EntryId = entryId;
            IsChunk = true;
            ChunkIndex = chunkIndex;
        }
    }
}
