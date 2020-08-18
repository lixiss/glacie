using System;

namespace Glacie.Data.Arz.Infrastructure
{
    // TODO: (High) (ArzFileLayout) Document possible values, used by various game engines
    // e.g.: TQ: RecordHasDecompressedSize | UseZlibCompression
    //       TQAE: UseZlibCompression
    //       GD: RecordHasDecompressedSize | UseLz4Compression

    // TODO: (High) (ArzFileLayout) Simplify names.

    /// <summary>
    /// Represent possible minor variations of ARZ file.
    /// </summary>
    [Flags]
    public enum ArzFileLayout
    {
        None = 0,

        /// <summary>
        /// Determines if ARZ record has "decompressed size" field.
        /// This flag specifies only presense of this field in structure,
        /// but doesn't enforce what this field should be actually used.
        /// </summary>
        RecordHasDecompressedSize = 1 << 0,

        /// <summary>
        /// Field data is zlib stream.
        /// </summary>
        UseZlibCompression = 1 << 2,

        /// <summary>
        /// ARZ field data is LZ4 block.
        /// </summary>
        UseLz4Compression = 1 << 3,
    }
}
