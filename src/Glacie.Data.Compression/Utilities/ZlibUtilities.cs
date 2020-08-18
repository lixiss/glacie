using System;

namespace Glacie.Data.Compression.Utilities
{
    public static class ZlibUtilities
    {
        /// <summary>
        /// Returns <c>true</c> if CMF and FLG bytes meet RFC1950 requirements.
        /// Note, what result may be used only as estimation, random input will result in false-positive answers.
        /// However, when this methods returns <c>false</c> this means what there is exactly is not ZLIB stream.
        /// See <a href="https://tools.ietf.org/html/rfc1950">ZLIB Compressed Data Format Specification version 3.3</a>.
        /// </summary>
        public static bool IsRfc1950StreamHeader(ReadOnlySpan<byte> span)
        {
            if (span.Length < 2) return false;
            return IsRfc1950StreamHeader(span[0], span[1]);
        }

        /// <inheritdoc cref="IsRfc1950StreamHeader(ReadOnlySpan{byte})"/>
        public static bool IsRfc1950StreamHeader(byte cmf, byte flg)
        {
            var cm = cmf & 0x0F;
            var cinfo = (cmf & 0xF0) >> 4;

            return cm == 0x08 // is deflate
                && cinfo <= 7 // is valid window size for deflate
                && (cmf * 256 + flg) % 31 == 0; // fcheck is valid
        }
    }
}
