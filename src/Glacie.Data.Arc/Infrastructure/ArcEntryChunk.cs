namespace Glacie.Data.Arc.Infrastructure
{
    public struct ArcEntryChunk
    {
        public const int Size = 12;

        private uint _offset;
        private int _compressedLength;
        private int _length;

        internal ArcEntryChunk(uint offset, int compressedLength, int length)
        {
            _offset = offset;
            _compressedLength = compressedLength;
            _length = length;
        }

        public uint Offset
        {
            readonly get => _offset;
            set => _offset = value;
        }

        public int CompressedLength
        {
            readonly get => _compressedLength;
            set => _compressedLength = value;
        }

        public int Length
        {
            readonly get => _length;
            set => _length = value;
        }

        /// <summary>
        /// When <see langword="true"/>, indicates that uncompressed data
        /// <strong>may be</strong> stored by this chunk. Actual storing mode
        /// determined by file format in this case.
        /// <br/>
        /// When <see langword="false"/>, indicates that compressed data stored by this chunk.
        /// </summary>
        public readonly bool MaybeStore
        {
            get => _compressedLength == _length;
        }
    }
}
