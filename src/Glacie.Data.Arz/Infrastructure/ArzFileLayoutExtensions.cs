using System.Runtime.CompilerServices;

namespace Glacie.Data.Arz.Infrastructure
{
    // TODO: (Low) (ArzFileLayout) When ArzFileLayout was a struct, it was better API.
    // TODO: (Low) (ArzFileLayout) Deal with AggressiveInlining.

    public static class ArzFileLayoutExtensions
    {
        // TODO: [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RecordHasDecompressedSize(this ArzFileLayout value)
            => (value & ArzFileLayout.RecordHasDecompressedSize) != 0;

        // TODO: [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasCompressionAlgorithm(this ArzFileLayout value)
            => (value & (ArzFileLayout.UseZlibCompression | ArzFileLayout.UseLz4Compression)) != 0;

        public static bool CompressionAlgorithmCompatibleWith(this ArzFileLayout value, ArzFileLayout other)
        {
            var thisAlg = value & (ArzFileLayout.UseZlibCompression | ArzFileLayout.UseLz4Compression);
            var otherAlg = other & (ArzFileLayout.UseZlibCompression | ArzFileLayout.UseLz4Compression);
            return thisAlg == otherAlg;
        }

        // TODO: [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldEmitDecompressedSize(this ArzFileLayout value)
            => (value & ArzFileLayout.UseLz4Compression) != 0;

        public static bool IsComplete(this ArzFileLayout value)
            => HasCompressionAlgorithm(value);

        /// <summary>
        /// Returns <c>true</c> if <c>value</c> has supported flags combination.
        /// </summary>
        public static bool IsValid(this ArzFileLayout value)
        {
            var compressionAlgorithm =
                value & (ArzFileLayout.UseZlibCompression | ArzFileLayout.UseLz4Compression);

            if (compressionAlgorithm == 0)
            {
                return true;
            }
            else if (compressionAlgorithm == ArzFileLayout.UseZlibCompression)
            {
                return true;
            }
            else if (compressionAlgorithm == ArzFileLayout.UseLz4Compression)
            {
                var hasDecompressedSize = (value & ArzFileLayout.RecordHasDecompressedSize) != 0;
                return hasDecompressedSize;
            }

            return false;
        }
    }
}
