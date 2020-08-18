using System;
using Glacie.Buffers;

namespace Glacie.Data.Compression
{
    public abstract class Encoder : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public abstract byte[] EncodeToArray(ReadOnlySpan<byte> input);

        public abstract DataBuffer EncodeToBuffer(ReadOnlySpan<byte> input);
    }
}
