namespace Glacie.Data.Arc
{
    internal struct ArcFileHeader
    {
        public const int Size = 7 * 4;

        /// <summary>
        /// 'ARC ' (0x00435241)
        /// </summary>
        public uint Magic { readonly get; set; }

        /// <summary>
        /// 1 - for Titan Quest, Titan Quest Immortal Throne, Titan Quest Anniversary Edition.
        /// 3 - for Grim Dawn.
        /// </summary>
        public int Version { readonly get; set; }

        public int EntryCount { readonly get; set; }

        public int ChunkCount { readonly get; set; }

        public uint ChunkTableLength { readonly get; set; }

        public uint StringTableLength { readonly get; set; }

        public uint ChunkTableOffset { readonly get; set; }

        public uint GetTocOffset()
        {
            return checked(GetStringTableOffset() + StringTableLength);
        }

        public uint GetStringTableOffset()
        {
            return checked(ChunkTableOffset + ChunkTableLength);
        }
    }
}
