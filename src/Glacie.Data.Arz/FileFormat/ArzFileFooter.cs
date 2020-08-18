namespace Glacie.Data.Arz.FileFormat
{
    // TODO: (Glacie.Data.Arz.FileFormat) This types should be internal.

    public struct ArzFileFooter
    {
        public const int Size = 16;

        public uint Hash { readonly get; set; }
        public uint StringTableHash { readonly get; set; }
        public uint RecordDataHash { readonly get; set; }
        public uint RecordTableHash { readonly get; set; }
    }
}
