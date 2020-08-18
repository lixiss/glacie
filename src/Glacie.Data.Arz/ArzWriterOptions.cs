using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz
{
    public sealed class ArzWriterOptions
    {
        public ArzWriterOptions()
        { }

        public ArzWriterOptions(bool optimize)
        {
            OptimizeStringTable = optimize;
            ComputeChecksum = optimize;
        }

        // TODO: (VeryLow) (ArzWriter) (Undecided) Hybrid mode? How to implement it.

        /// <summary>
        /// When <c>true</c>, writes only changed records.
        /// When <c>false</c>, writes any records in database.
        /// </summary>
        public bool ChangesOnly { get; set; } = false;

        /// <summary>
        /// When enabled, performs encoding of field data in multiple threads.
        /// </summary>
        public bool Multithreaded { get; set; } = true;

        public int MaxDegreeOfParallelism { get; set; } = -1;

        /// <summary>
        /// When enabled, for zlib compressed data uses <c>libdeflate</c>
        /// instead of <see cref="System.IO.Compression.DeflateStream" />.
        /// <c>libdeflate</c> should provide better performance.
        /// </summary>
        public bool? UseLibDeflate { get; set; } = null;

        public ArzFileLayout Layout { get; set; } = ArzFileLayout.None;

        // TODO: (Medium) (ArzWriter) InferRecordClass - not sure about this feature. Generally correct code should not rely on this option.
        public bool InferRecordClass { get; set; } = true;

        /// <summary>
        /// When <c>true</c>, writer will create new string table, so it
        /// will contain only actually used entries, however, to achieve this
        /// writer will need to re-encode all field data, and compress them (so
        /// <see cref="ForceCompression"/> will not taken into account in this
        /// case).
        /// When <c>false</c>, writer will use database's string table.
        /// </summary>
        public bool OptimizeStringTable { get; set; } = false;

        /// <summary>
        /// When <c>true</c>, writer will compress all records.
        /// When <c>false</c>, writer will prefer to reuse raw (compressed)
        /// field data blocks whenever is possible.
        /// </summary>
        public bool ForceCompression { get; set; } = false;

        // TODO: (Medium) (ArzWriter) Use special enumeration for CompressionLevel which will be easier to understand. E.g. fastest/optimal/maximum.
        // Also need sensible defaults (which might depend on encoder).
        // Also may be useful if encoder will be able report to supported compression levels (DeflateStream support NoCompression, Fastest, Optimal, 
        // while libdeflate supports 1-12 and Lz4 0,3-12 (however 1-2 looks like do something too)).
        /// <summary>
        /// Specifies compression level (1..12).
        /// </summary>
        public int CompressionLevel { get; set; } = 0;

        public bool ComputeChecksum { get; set; } = false;
    }
}
