using System;
using Glacie.Data.Compression.Utilities;
using IO = System.IO;
using LZ4I = K4os.Compression.LZ4;

namespace Glacie.Data.Compression
{
    public sealed class Lz4Decoder : Decoder
    {
        private const int MaxInputBufferOnStackSize = 7 * 1024;

        private static readonly Lz4Decoder s_decoder = new Lz4Decoder();

        public static Lz4Decoder Shared => s_decoder;

        public Lz4Decoder() { }

        protected override void Dispose(bool disposing) { }

        public override byte[] Decode(ReadOnlySpan<byte> source, int decompressedSize = 0)
        {
            Check.True(decompressedSize > 0);
            // TODO: [Low] Support decoding with unknown decompressed size.

            var target = Multitargeting.AllocateUninitializedByteArray(decompressedSize);

            Span<byte> targetSpan = target;
            var bytesWritten = LZ4I.LZ4Codec.Decode(source, targetSpan);
            Check.True(bytesWritten == decompressedSize);

            return target;
        }

        public override byte[] Decode(IO.Stream source, int sourceLength, int decompressedSize = 0)
        {
            byte[]? rentedSourceBuffer = null;
            try
            {
                Span<byte> sourceBuffer = sourceLength <= MaxInputBufferOnStackSize
                    ? stackalloc byte[MaxInputBufferOnStackSize]
                    : (rentedSourceBuffer = SharedBufferPool.Rent(sourceLength));
                var sourceSpan = sourceBuffer.Slice(0, sourceLength);

                var bytesRead = source.Read(sourceSpan);
                Check.True(bytesRead == sourceLength);

                return Decode(sourceSpan, decompressedSize);
            }
            finally
            {
                if (!(rentedSourceBuffer is null))
                {
                    SharedBufferPool.Return(rentedSourceBuffer);
                }
            }
        }
    }
}
