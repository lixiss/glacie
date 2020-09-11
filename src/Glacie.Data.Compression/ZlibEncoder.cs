using System;
using Glacie.Buffers;
using Glacie.ChecksumAlgorithms;
using IO = System.IO;

namespace Glacie.Data.Compression
{
    /// <inheritdoc />
    public sealed class ZlibEncoder : Encoder
    {
        private const int MaxInputBufferOnStackSize = 7 * 1024;
        private const int ZlibDeflateOutputBufferOnStackSize = 16 * 1024;

        private IO.Compression.CompressionLevel _compressionLevel;

        public ZlibEncoder(CompressionLevel compressionLevel)
        {
            _compressionLevel = MapCompressionLevel(compressionLevel);
        }

        protected override void Dispose(bool disposing) { }

        public override byte[] EncodeToArray(ReadOnlySpan<byte> input)
        {
            using (var memoryStream = new IO.MemoryStream())
            {
                WriteZlibStream(memoryStream, input, out var bytesWritten);
                DebugCheck.True(memoryStream.Length == bytesWritten);

                if (memoryStream.TryGetBuffer(out var buffer))
                {
                    if (buffer.Count == buffer.Array.Length)
                    {
                        return buffer.Array;
                    }
                }

                return memoryStream.ToArray();
            }
        }

        public override DataBuffer EncodeToBuffer(ReadOnlySpan<byte> input)
        {
            var compressBound = GetCompressBound(input.Length);
            var outputBuffer = DataBuffer.Rent(compressBound);
            try
            {
                using (var memoryStream = new IO.MemoryStream(outputBuffer.Array, true))
                {
                    WriteZlibStream(memoryStream, input, out var bytesWritten);

                    var resultBuffer = outputBuffer.WithLength(bytesWritten);
                    outputBuffer = default;
                    return resultBuffer;
                }
            }
            finally
            {
                outputBuffer.Return();
            }
        }

        private unsafe void WriteZlibStream(IO.Stream target, ReadOnlySpan<byte> source, out int bytesWritten)
        {
            var checksum = Adler32.ComputeHash(source);

            if (source.Length == 0)
            {
                // TODO: (Low) ZlibEncoder can't compress block with zero length.
                // libdeflate easily do this, se here we can emit this block as raw bytes manually.
                throw Error.InvalidOperation("This codec can't encode zero sized block.");
            }

            fixed (byte* pSource = &source[0])
            {
                using (var sourceStream = new IO.UnmanagedMemoryStream(pSource, source.Length))
                {
                    WriteZlibStream(target, sourceStream, checksum, out bytesWritten);
                }
            }
        }

        private void WriteZlibStream(IO.Stream output, IO.Stream input, uint adler32, out int bytesWritten)
        {
            if (!output.CanSeek) throw Error.InvalidOperation();

            var positionBefore = output.Position;

            output.WriteByte(0x78); // cmf
            output.WriteByte(0x5E); // flg

            using (var codec = new IO.Compression.DeflateStream(output, _compressionLevel, leaveOpen: true))
            {
                input.CopyTo(codec);
            }

            output.WriteByte((byte)(adler32 >> 24));
            output.WriteByte((byte)(adler32 >> 16));
            output.WriteByte((byte)(adler32 >> 8));
            output.WriteByte((byte)(adler32 >> 0));

            var positionAfter = output.Position;

            bytesWritten = checked((int)(positionAfter - positionBefore));
        }

        private static int GetCompressBound(int inputLength)
        {
            return 6 + inputLength + 1 + Math.Max((inputLength + 10000 - 1) / 10000, 1) * 5 + 8;
        }

        private static IO.Compression.CompressionLevel MapCompressionLevel(CompressionLevel compressionLevel)
        {
            // TODO: (Low) Provide information about supported compression levels by encoder.
            if ((int)compressionLevel < 6) return IO.Compression.CompressionLevel.Fastest;
            else return IO.Compression.CompressionLevel.Optimal;
        }
    }
}
