using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz
{
    public sealed class ArzReaderOptions
    {
        public ArzReadingMode Mode { get; set; } = ArzReadingMode.Raw;

        public ArzFileFormat Format { get; set; } = ArzFileFormat.Automatic;

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

        /// <summary>
        /// <para>If <see langword="true"/>, database will close underlying file
        /// or stream when it no more needed (this happens exactly when database
        /// is opened).</para>
        /// <para>If <see langword="false"/>, database will keep underlying file
        /// or stream opened until it disposed.</para>
        /// </summary>
        /// <remarks>
        /// <para>Note, what this mode available only in
        /// <see cref="ArzReadingMode.Raw"/> or
        /// <see cref="ArzReadingMode.Full"/> modes, and writer will not be
        /// able to reuse already compressed data (so it will be forced to
        /// re-compress records).</para>
        /// <para>This is useful option when you doesn't want lock input stream,
        /// and doesn't need to write or recompression is already intended
        /// (e.g. like you set <see cref="ArzWriterOptions.ForceCompression"/>).
        /// </para>
        /// </remarks>
        public bool CloseUnderlyingStream { get; set; } = false;
    }
}
