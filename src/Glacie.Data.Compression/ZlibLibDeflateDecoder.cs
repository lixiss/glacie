using System;
using System.Runtime.InteropServices;
using Glacie.Data.Compression.Utilities;
using IO = System.IO;
using size_t = System.UIntPtr;

namespace Glacie.Data.Compression
{
    public sealed class ZlibLibDeflateDecoder : Decoder
    {
        private const int InputBufferStackSize = 8 * 1024;
        private const int OutputBufferStackSize = 16 * 1024;
        private const int MaxOutputRatio = 50;

        public static bool IsSupported => LibDeflatePlatformSupport.IsSupported;

        private IntPtr _decompressor;
        private int _outputRatio = 3;

        public ZlibLibDeflateDecoder()
        {
            var decompressor = Interop.LibDeflate.libdeflate_alloc_decompressor();
            if (decompressor == IntPtr.Zero) throw Error.InvalidOperation("Failed to allocate decompressor.");
            _decompressor = decompressor;
        }

        ~ZlibLibDeflateDecoder()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_decompressor != IntPtr.Zero)
            {
                Interop.LibDeflate.libdeflate_free_decompressor(_decompressor);
                _decompressor = IntPtr.Zero;
            }
        }

        public override byte[] Decode(ReadOnlySpan<byte> source, int decompressedSize)
        {
            DecodeInternal(source, decompressedSize, out var outputBuffer);
            if (decompressedSize > 0 && outputBuffer.Length != decompressedSize)
                throw Error.InvalidOperation("Invalid decompressed size.");
            return outputBuffer;
        }

        public override byte[] Decode(IO.Stream source, int sourceLength, int decompressedSize = 0)
        {
            byte[]? rentedInputBuffer = null;
            try
            {
                Span<byte> inputBuffer = sourceLength <= InputBufferStackSize
                    ? stackalloc byte[InputBufferStackSize]
                    : (rentedInputBuffer = SharedBufferPool.Rent(sourceLength));
                var inputSpan = inputBuffer.Slice(0, sourceLength);

                var bytesRead = source.Read(inputSpan);
                Check.True(bytesRead == sourceLength);

                return Decode(inputSpan, decompressedSize);
            }
            finally
            {
                if (!(rentedInputBuffer is null))
                {
                    SharedBufferPool.Return(rentedInputBuffer);
                }
            }
        }

        private unsafe void DecodeInternal(ReadOnlySpan<byte> source, int decompressedSize, out byte[] target)
        {
            if (decompressedSize > 0)
            {
                var output = Multitargeting.AllocateUninitializedByteArray(decompressedSize);
                if (TryDecode(source, output, out var decodedLength))
                {
                    target = output;
                    return;
                }
                else throw Error.Unreachable();
            }
            else
            {
                if (source.Length * _outputRatio <= OutputBufferStackSize)
                {
                    var outputBuffer = stackalloc byte[OutputBufferStackSize];
                    if (TryDecode(source, new Span<byte>(outputBuffer, OutputBufferStackSize), out var decodedLength))
                    {
                        var outputSpan = new Span<byte>(outputBuffer, decodedLength);
                        target = outputSpan.ToArray();
                        return;
                    }
                }

                while (_outputRatio <= MaxOutputRatio)
                {
                    var rentedBuffer = SharedBufferPool.Rent(source.Length * _outputRatio);
                    try
                    {
                        if (TryDecode(source, new Span<byte>(rentedBuffer, 0, rentedBuffer.Length), out var decodedLength))
                        {
                            var outputSpan = new Span<byte>(rentedBuffer, 0, decodedLength);
                            target = outputSpan.ToArray();
                            return;
                        }
                    }
                    finally
                    {
                        SharedBufferPool.Return(rentedBuffer);
                    }
                    _outputRatio++;
                }

                throw Error.InvalidOperation("Output Buffer Ratio reached limit.");
            }
        }

        private unsafe bool TryDecode(ReadOnlySpan<byte> input, Span<byte> output, out int decodedLength)
        {
            size_t inputBytesConsumed;
            size_t actualSize;
            fixed (byte* pSource = &MemoryMarshal.GetReference(input))
            fixed (byte* pOutput = &MemoryMarshal.GetReference(output))
            {
                var result = Interop.LibDeflate.libdeflate_zlib_decompress_ex(_decompressor,
                    pSource, (size_t)input.Length,
                    pOutput, (size_t)output.Length,
                    out inputBytesConsumed, out actualSize);

                if (result == Interop.LibDeflate.libdeflate_result.LIBDEFLATE_SUCCESS)
                {
                    Check.True((int)inputBytesConsumed == input.Length);
                    decodedLength = (int)actualSize;
                    return true;
                }
                else if (result == Interop.LibDeflate.libdeflate_result.LIBDEFLATE_INSUFFICIENT_SPACE)
                {
                    decodedLength = 0;
                    return false;
                }
                else throw Error.InvalidOperation("Error: {0}", result.ToString());
            }
        }
    }
}
