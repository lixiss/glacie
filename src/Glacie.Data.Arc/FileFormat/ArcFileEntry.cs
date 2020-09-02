namespace Glacie.Data.Arc
{
    internal struct ArcFileEntry
    {
        public const int Size = 44;

        public ArcFileEntryType EntryType { readonly get; set; }

        /// <remarks>
        /// Valid if <see cref="EntryType"/> is <see cref="ArcFileEntryType.Store"/>.
        /// Offset is relative to begin of archive's file. (E.g. absolute position.)
        /// </remarks>
        public uint Offset { readonly get; set; }
        public uint CompressedLength { readonly get; set; }
        public uint Length { readonly get; set; }

        /// <summary>
        ///  Adler32 hash of the non-compressed bytes.
        /// </summary>
        public uint Hash { readonly get; set; }
        public long Timestamp { readonly get; set; }
        public int ChunkCount { readonly get; set; }
        public int ChunkIndex { readonly get; set; }
        public int NameStringLength { readonly get; set; }
        public uint NameStringOffset { readonly get; set; }
    }
}
