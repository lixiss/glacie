using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz.FileFormat
{
    public struct ArzFileRecord
    {
        public arz_string_id NameId { readonly get; set; }

        public int ClassId { readonly get; set; }

        public int DataOffset { readonly get; set; }

        // TODO: compressedDataSize
        public int DataSize { readonly get; set; }

        // TODO: decompressedDataSize
        public int DataSizeDecompressed { readonly get; set; }

        public long Timestamp { readonly get; set; }
    }
}
