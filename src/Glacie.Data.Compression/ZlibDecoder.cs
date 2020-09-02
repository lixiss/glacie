using System;
using Glacie.Data.Compression.Utilities;
using IO = System.IO;

namespace Glacie.Data.Compression
{
    /// <inheritdoc />
    public sealed class ZlibDecoder : Decoder
    {
        private const int MaxInputBufferOnStackSize = 7 * 1024;
        private const int ZlibDeflateOutputBufferOnStackSize = 16 * 1024;

        private static readonly ZlibDecoder s_shared = new ZlibDecoder();

        public static ZlibDecoder Shared => s_shared;

        public ZlibDecoder() { }

        protected override void Dispose(bool disposing) { }

        public unsafe override byte[] Decode(ReadOnlySpan<byte> source, int decompressedSize)
        {
            // TODO: (Low) Validate arguments decompressedSize >= 0
            if (!ZlibUtilities.IsRfc1950StreamHeader(source))
            {
                throw Error.InvalidOperation("Not a zlib stream.");
            }

            if (source.Length < 6) throw Error.InvalidOperation("Not a zlib stream.");

            // We skip two bytes of header.
            fixed (byte* pSource = &source[2])
            {
                using (var stream = new IO.UnmanagedMemoryStream(pSource, source.Length - 2))
                {
                    return ReadZlibDeflateStream(stream, decompressedSize);
                }
            }
        }

        public override byte[] Decode(IO.Stream source, int sourceLength, int decompressedSize = 0)
        {
            return ReadZlibStream(source, sourceLength, decompressedSize);
        }

        public override unsafe void Decode(ReadOnlySpan<byte> input, Span<byte> output)
        {
            if (!ZlibUtilities.IsRfc1950StreamHeader(input))
            {
                throw Error.InvalidOperation("Not a zlib stream.");
            }

            if (input.Length < 6) throw Error.InvalidOperation("Not a zlib stream.");

            // We skip two bytes of header.
            fixed (byte* pInput = &input[2])
            fixed (byte* pOutput = &output[0])
            {
                using (var inputStream = new IO.UnmanagedMemoryStream(pInput, input.Length - 2))
                using (var outputStream = new IO.UnmanagedMemoryStream(pOutput, output.Length, output.Length, IO.FileAccess.Write))
                using (var inputDeflateStream = new IO.Compression.DeflateStream(inputStream,
                    IO.Compression.CompressionMode.Decompress, leaveOpen: false))
                {
                    inputDeflateStream.CopyTo(outputStream);
                    Check.True(outputStream.Position == output.Length);
                }
            }
        }

        public override void Decode(IO.Stream input, int inputLength, Span<byte> output)
        {
            // TODO: (VeryLow) (ZlibDecoder) Provide better implementation.
            base.Decode(input, inputLength, output);
        }

        private static byte[] ReadZlibStream(IO.Stream source, int sourceLength, int decompressedSize = 0)
        {
            // DeflateStream implementation hold another data buffer, so as result it will consume
            // more bytes from source stream than really need. To prevent this, we doesn't allow do
            // this by wrapping source stream, which virtually holds only specified number of bytes.

            // TODO: (VeryLow) There is possible to pool this temporary streams and reuse them,
            // but feel what there is better just to go with `libdeflate` when possible.

            using (var sourceSegmentStream = new IO.ReadOnlySegmentStream(source, sourceLength))
            {
                var cmf = (byte)sourceSegmentStream.ReadByte();
                var flg = (byte)sourceSegmentStream.ReadByte();
                if (!ZlibUtilities.IsRfc1950StreamHeader(cmf, flg))
                {
                    throw Error.InvalidOperation("Given stream is not a zlib stream.");
                }

                return ReadZlibDeflateStream(sourceSegmentStream, decompressedSize);
            }
        }

        private static byte[] ReadZlibDeflateStream(IO.Stream source, int decompressedSize)
        {
            if (decompressedSize == 0)
            {
                return ReadZlibDeflateStream(source);
            }

            var outputBuffer = Multitargeting.AllocateUninitializedByteArray(decompressedSize);

            using (var inputStream = new IO.Compression.DeflateStream(source,
                IO.Compression.CompressionMode.Decompress, leaveOpen: true))
            {
                int bytesRead;
                Span<byte> outputSpan = outputBuffer;
                while ((bytesRead = inputStream.Read(outputSpan)) > 0)
                {
                    outputSpan = outputSpan.Slice(bytesRead);
                }

                Check.True(outputSpan.IsEmpty);
            }

            return outputBuffer;
        }

        private static byte[] ReadZlibDeflateStream(IO.Stream source)
        {
            // TODO: (Low) Logically DeflateStream should not consume Adler32 checksum, but
            // it probably gets lost in DeflateStream internal buffer. Should we do something with this?

            Span<byte> buffer = stackalloc byte[ZlibDeflateOutputBufferOnStackSize];
            var bytesInBuffer = 0;

            using (var inputStream = new IO.Compression.DeflateStream(source,
                IO.Compression.CompressionMode.Decompress, leaveOpen: true))
            {
                // Read data until buffer is completely filled.
                {
                    int bytesRead;
                    var outputBufferSpan = buffer;
                    while ((bytesRead = inputStream.Read(outputBufferSpan)) > 0)
                    {
                        outputBufferSpan = outputBufferSpan.Slice(bytesRead);
                        bytesInBuffer += bytesRead;
                    }
                }

                // If buffer has less data than buffer size -> all data was read.
                if (bytesInBuffer < buffer.Length)
                {
                    // Buffer holds all data
                    return buffer.Slice(0, bytesInBuffer).ToArray();
                }

                // Slow path
                using (var outputStream = new IO.MemoryStream())
                {
                    outputStream.Write(buffer.Slice(0, bytesInBuffer));

                    int bytesRead;
                    while ((bytesRead = inputStream.Read(buffer)) > 0)
                    {
                        outputStream.Write(buffer.Slice(0, bytesRead));
                    }

                    if (outputStream.TryGetBuffer(out var resultBuffer))
                    {
                        if (resultBuffer.Count == resultBuffer.Array!.Length)
                        {
                            return resultBuffer.Array;
                        }
                        else
                        {
                            // TODO: (VeryLow) (PerformanceMetrics) # of buffer reallocations in ReadDeflateStream
                            return resultBuffer.ToArray();
                        }
                    }
                    else throw Error.Unreachable();
                }
            }
        }
    }
}
