using System;

namespace Glacie.ChecksumAlgorithms
{
    // Based on "Fast Computation of Adler32 Checksums"
    // https://software.intel.com/content/www/us/en/develop/articles/fast-computation-of-adler32-checksums.html
    // TODO: (VeryLow) (Adler32) Vectorized version is not implemented.

    // TODO: (Low) Redesign Adler32 helper. It doesn't need to be class at all.
    // State too simple, and often doesn't need. Also it is only efficient when
    // calculated over reasonable big blocks. How to express it. Over struct?
    // Make it as ref-only struct, to prevent misuse?

    /// <summary>
    /// Adler32 checksum calculator.
    /// </summary>
    public sealed class Adler32
    {
        private const int Modulo = 65521;

        private uint _hash;

        public Adler32(uint initialHash = 1)
        {
            _hash = initialHash;
        }

        public uint Hash => _hash;

        /// <summary>
        /// Computes the hash value for the input data.
        /// </summary>
        public uint ComputeHash(byte[] buffer)
        {
            return ComputeHash(buffer.AsSpan());
        }

        /// <summary>
        /// Computes the hash value for the input data.
        /// </summary>
        public uint ComputeHash(ArraySegment<byte> buffer)
        {
            return ComputeHash(buffer.AsSpan());
        }

        /// <summary>
        /// Computes the hash value for the input data.
        /// </summary>
        public uint ComputeHash(byte[] buffer, int offset, int count)
        {
            return ComputeHashCore(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        /// <summary>
        /// Computes the hash value for the input data.
        /// </summary>
        public uint ComputeHash(ReadOnlySpan<byte> data)
        {
            return ComputeHashCore(data);
        }

        private uint ComputeHashCore(ReadOnlySpan<byte> data)
        {
            var offset = 0;
            var count = data.Length;

            uint s1 = _hash & 0xFFFF;
            uint s2 = _hash >> 16;

            while (count > 0)
            {
                var n = Math.Min(count, 5552);
                count -= n;

                while (n > 0)
                {
                    s1 = s1 + data[offset];
                    s2 = s2 + s1;

                    offset++;
                    n--;
                }

                s1 %= Modulo;
                s2 %= Modulo;
            }

            return _hash = (s2 << 16) | s1;
        }

        /// <summary>
        /// </summary>
        public static uint ComputeHash(ReadOnlySpan<byte> data, uint initialHash = 1)
        {
            var offset = 0;
            var count = data.Length;

            uint s1 = initialHash & 0xFFFF;
            uint s2 = initialHash >> 16;

            while (count > 0)
            {
                var n = Math.Min(count, 5552);
                count -= n;

                while (n > 0)
                {
                    s1 = s1 + data[offset];
                    s2 = s2 + s1;

                    offset++;
                    n--;
                }

                s1 %= Modulo;
                s2 %= Modulo;
            }

            return (s2 << 16) | s1;
        }
    }
}
