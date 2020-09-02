namespace Glacie.Data.Arc.Infrastructure
{
    internal struct FileSegment
    {
        public long Offset;
        public long Length;

        public FileSegment(long offset, long length)
        {
            Offset = offset;
            Length = length;
        }
    }
}
