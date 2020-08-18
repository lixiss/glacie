namespace Glacie.Data.Arz.FileFormat
{
    public struct ArzFileHeader
    {
        public const int Size = 24;

        public ushort Magic { readonly get; set; }
        public ushort Version { readonly get; set; }
        public int RecordTableOffset { readonly get; set; }
        public int RecordTableSize { readonly get; set; }
        public int RecordTableCount { readonly get; set; }
        public int StringTableOffset { readonly get; set; }
        public int StringTableSize { readonly get; set; }
    }
}
