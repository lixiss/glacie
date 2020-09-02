using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;

namespace Glacie.Data.Arc
{
    public sealed class ArcArchiveOptions
    {
        public ArcArchiveMode Mode { get; set; } = ArcArchiveMode.Read;

        public ArcFileFormat Format { get; set; } = default;

        // TODO: (ArcArchive) Which default compression level to use? They are might be slighty different depends on encoder.
        /// <summary>
        /// Specifies default compression level.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Maximum;

        /// <summary>
        /// If <see langword="true"/>, then writing to the file will not overwrite catalog
        /// or blocks which currently in use. This will result in bigger file, which will need
        /// to be compacted later. However if error occurs during writing, original file
        /// data will not be corrupted.
        /// <br/>
        /// If <see langword="false"/>, then writing to the file may overwrite
        /// any data which no more in use, but errors during writing may result
        /// in corrupted file.
        /// </summary>
        public bool SafeWrite { get; set; } = false;

        /// <summary>
        /// <para>Actual default value is 2048 (2KiB).</para>
        /// </summary>
        public int? HeaderAreaLength { get; set; } = default;

        /// <summary>
        /// <para>Entries can be stored in different modes:
        /// store (uncompressed linear block), chunked (possibly compressed
        /// part of data). This option affect only when you write new entries.</para>
        /// <para>Default value is 262144 (256KiB).</para>
        /// </summary>
        public int? ChunkLength { get; set; } = default;

        // TODO: (Low) (Arc/Arz) Write better description.
        /// <summary>
        /// When enabled, for zlib compressed data uses <c>libdeflate</c>
        /// instead of <see cref="System.IO.Compression.DeflateStream" />.
        /// <c>libdeflate</c> should provide better performance.
        /// </summary>
        public bool? UseLibDeflate { get; set; } = default;
    }
}
