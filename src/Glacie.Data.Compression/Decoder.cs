using System;
using IO = System.IO;

namespace Glacie.Data.Compression
{
    public abstract class Decoder : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public abstract byte[] Decode(ReadOnlySpan<byte> input, int outputLength = 0);

        public abstract byte[] Decode(IO.Stream input, int inputLength, int outputLength = 0);
    }
}
