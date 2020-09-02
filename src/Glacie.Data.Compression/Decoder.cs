using System;
using Glacie.Data.Compression.Utilities;
using IO = System.IO;

namespace Glacie.Data.Compression
{
    public abstract class Decoder : IDisposable
    {
        private const int MaxInputBufferOnStackSize = 8 * 1024;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public abstract byte[] Decode(ReadOnlySpan<byte> input, int outputLength = 0);

        public abstract byte[] Decode(IO.Stream input, int inputLength, int outputLength = 0);

        public abstract void Decode(ReadOnlySpan<byte> input, Span<byte> output);

        public virtual void Decode(IO.Stream input, int inputLength, Span<byte> output)
        {
            byte[]? rentedSourceBuffer = null;
            try
            {
                Span<byte> sourceBuffer = inputLength <= MaxInputBufferOnStackSize
                    ? stackalloc byte[MaxInputBufferOnStackSize]
                    : (rentedSourceBuffer = SharedBufferPool.Rent(inputLength));
                var inputSpan = sourceBuffer.Slice(0, inputLength);

                var bytesRead = input.Read(inputSpan);
                Check.True(bytesRead == inputLength);

                Decode(inputSpan, output);
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
