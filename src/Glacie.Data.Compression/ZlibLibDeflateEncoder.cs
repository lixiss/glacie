using System;
using System.Runtime.InteropServices;
using Glacie.Buffers;
using Glacie.Data.Compression.Utilities;
using size_t = System.UIntPtr;

namespace Glacie.Data.Compression
{
    /// <inheritdoc />
    public sealed class ZlibLibDeflateEncoder : Encoder
    {
        private const int OutputBufferStackSize = 16 * 1024;

        public static bool IsSupported => LibDeflatePlatformSupport.IsSupported;

        private IntPtr _compressor;

        public ZlibLibDeflateEncoder(int compressionLevel)
        {
            var compressor = Interop.LibDeflate.libdeflate_alloc_compressor(MapCompressionLevel(compressionLevel));
            if (compressor == IntPtr.Zero) throw Error.InvalidOperation("Failed to allocated compressor.");
            _compressor = compressor;
        }

        protected override void Dispose(bool disposing)
        {
            if (_compressor != IntPtr.Zero)
            {
                Interop.LibDeflate.libdeflate_free_compressor(_compressor);
                _compressor = IntPtr.Zero;
            }
        }

        public override unsafe byte[] EncodeToArray(ReadOnlySpan<byte> input)
        {
            var compressBound = GetCompressBound(input.Length);
            if (compressBound <= OutputBufferStackSize)
            {
                var outputBuffer = stackalloc byte[OutputBufferStackSize];
                var outputLength = EncodeBuffer(input, new Span<byte>(outputBuffer, OutputBufferStackSize));

                var outputSpan = new Span<byte>(outputBuffer, outputLength);
                return outputSpan.ToArray();
            }
            else
            {
                var outputBuffer = SharedBufferPool.Rent(compressBound);
                try
                {
                    var outputLength = EncodeBuffer(input, outputBuffer);

                    var outputSpan = new Span<byte>(outputBuffer, 0, outputLength);
                    return outputSpan.ToArray();
                }
                finally
                {
                    SharedBufferPool.Return(outputBuffer);
                }
            }
        }

        public override DataBuffer EncodeToBuffer(ReadOnlySpan<byte> input)
        {
            var compressBound = GetCompressBound(input.Length);

            var outputBuffer = DataBuffer.Rent(compressBound);
            try
            {
                var outputLength = EncodeBuffer(input, outputBuffer.Span);

                var resultBuffer = outputBuffer.WithLength(outputLength);
                outputBuffer = default;
                return resultBuffer;
            }
            finally
            {
                outputBuffer.Return();
            }
        }

        private unsafe int EncodeBuffer(ReadOnlySpan<byte> input, Span<byte> output)
        {
            fixed (byte* pInput = &MemoryMarshal.GetReference(input))
            fixed (byte* pOutput = &MemoryMarshal.GetReference(output))
            {
                var resultSize = Interop.LibDeflate.libdeflate_zlib_compress(_compressor,
                    pInput, (size_t)input.Length,
                    pOutput, (size_t)output.Length);

                if (resultSize == UIntPtr.Zero) throw Error.InvalidOperation("CompressZlib fail, insufficient output buffer?");

                var outputSize = checked((int)resultSize);
                return outputSize;
            }
        }

        private int GetCompressBound(int inputLength)
        {
            if (inputLength <= 32768)
            {
                return inputLength + 35;
            }

            var result = Interop.LibDeflate.libdeflate_zlib_compress_bound(_compressor, (size_t)inputLength);
            return (int)result;
        }

        private static int MapCompressionLevel(int compressionLevel)
        {
            if (compressionLevel < 1) return 1;
            else if (compressionLevel > 12) return 12;
            else return compressionLevel;
        }
    }
}
