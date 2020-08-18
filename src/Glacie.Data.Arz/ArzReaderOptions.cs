using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz
{
    public sealed class ArzReaderOptions
    {
        public ArzReadingMode Mode { get; set; } = ArzReadingMode.Raw;

        public ArzFileLayout Layout { get; set; } = ArzFileLayout.None;

        /// <summary>
        /// When enabled, performs decoding of field data (decompression) in
        /// multiple threads. However, this actually possible only when
        /// reading mode is full.
        /// </summary>
        public bool Multithreaded { get; set; } = true;

        public int MaxDegreeOfParallelism { get; set; } = -1;

        /// <summary>
        /// When enabled, for zlib compressed data uses <c>libdeflate</c>
        /// instead of <see cref="System.IO.Compression.DeflateStream" />.
        /// <c>libdeflate</c> should provide better performance.
        /// </summary>
        public bool? UseLibDeflate { get; set; } = null;
    }
}
