using System;
using Glacie.Buffers;
using Glacie.Data.Compression.Utilities;
using LZ4I = K4os.Compression.LZ4;

namespace Glacie.Data.Compression
{
    /// <inheritdoc />
    public sealed class Lz4Encoder : Encoder
    {
        private readonly LZ4I.LZ4Level _compressionLevel;

        public Lz4Encoder(CompressionLevel compressionLevel)
        {
            _compressionLevel = MapCompressionLevel(compressionLevel);
        }

        protected override void Dispose(bool disposing) { }

        public override byte[] EncodeToArray(ReadOnlySpan<byte> input)
        {
            var maxOutputSize = LZ4I.LZ4Codec.MaximumOutputSize(input.Length);

            var outputBuffer = SharedBufferPool.Rent(maxOutputSize);
            try
            {
                var bytesWritten = LZ4I.LZ4Codec.Encode(input, outputBuffer, _compressionLevel);
                if (bytesWritten < 0) throw Error.InvalidOperation("LZ4 encode failed.");

                var result = Multitargeting.AllocateUninitializedByteArray(bytesWritten);
                Array.Copy(outputBuffer, 0, result, 0, bytesWritten);
                return result;
            }
            finally
            {
                SharedBufferPool.Return(outputBuffer);
            }
        }

        public override DataBuffer EncodeToBuffer(ReadOnlySpan<byte> input)
        {
            var maxOutputSize = LZ4I.LZ4Codec.MaximumOutputSize(input.Length);

            var outputBuffer = DataBuffer.Rent(maxOutputSize);
            try
            {
                var bytesWritten = LZ4I.LZ4Codec.Encode(input, outputBuffer.Span, _compressionLevel);
                if (bytesWritten < 0) throw Error.InvalidOperation("LZ4 encode failed.");

                var resultBuffer = outputBuffer.WithLength(bytesWritten);
                outputBuffer = default;
                return resultBuffer;
            }
            finally
            {
                outputBuffer.Return();
            }
        }

        private static LZ4I.LZ4Level MapCompressionLevel(CompressionLevel compressionLevel)
        {
            // TODO: (Low) Lz4Encoder: Levels 1 and 2 is not mapped. Verify actual LZ4 implementation.
            switch ((int)compressionLevel)
            {
                case 0: return LZ4I.LZ4Level.L00_FAST;
                case 1: return (LZ4I.LZ4Level)1;
                case 2: return (LZ4I.LZ4Level)2;
                case 3: return LZ4I.LZ4Level.L03_HC;
                case 4: return LZ4I.LZ4Level.L04_HC;
                case 5: return LZ4I.LZ4Level.L05_HC;
                case 6: return LZ4I.LZ4Level.L06_HC;
                case 7: return LZ4I.LZ4Level.L07_HC;
                case 8: return LZ4I.LZ4Level.L08_HC;
                case 9: return LZ4I.LZ4Level.L09_HC;
                case 10: return LZ4I.LZ4Level.L10_OPT;
                case 11: return LZ4I.LZ4Level.L11_OPT;
                case 12: return LZ4I.LZ4Level.L12_MAX;
                default:
                    if ((int)compressionLevel < 1) return LZ4I.LZ4Level.L00_FAST;
                    else return LZ4I.LZ4Level.L12_MAX;
            }
        }
    }
}
