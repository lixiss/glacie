using System;
using System.Buffers;
using Glacie.Utilities;

namespace Glacie.Buffers
{
    /// <summary>
    /// Represents pooled or non-pooled data buffer.
    /// </summary>
    public struct DataBuffer : IDisposable
    {
        // We doesn't use ArrayBool<byte>.Shared because it bound to TLS,
        // while our common case is create buffer on one thread, and consume on
        // another.
        // TODO: (VeryLow) Create custom ArrayPool<byte>,
        // to utilize GC.AllocateUninitializedArray: in net5.0 it is somewhy
        // still doesn't use this call.
        private static readonly ArrayPool<byte> s_pool = ArrayPool<byte>.Create();

        private byte[]? _buffer;
        private int _length;
        private readonly bool _owned;

        private DataBuffer(byte[] buffer, int length, bool owned)
        {
            _buffer = buffer;
            _length = length;
            _owned = owned;
        }

        public void Dispose()
        {
            Return();
        }

        public byte[] Array => _buffer!;

        public int Length => _length;

        public bool Owned => _owned;

        public Span<byte> Span => new Span<byte>(_buffer, 0, _length);

        public DataBuffer WithLength(int length)
        {
            Check.True(_length >= length);
            return new DataBuffer(_buffer!, length, _owned);
        }

        public static DataBuffer Rent(int minimumLength)
        {
            var buffer = s_pool.Rent(minimumLength);
            return new DataBuffer(buffer, minimumLength, true);
        }

        /// <summary>
        /// Creates non-owned, buffer by pool.
        /// Calling Return is still valid, but effectively no-op.
        /// </summary>
        public static DataBuffer Create(int length)
        {
            return Create(ArrayUtilities.AllocateUninitializedArray<byte>(length), length);
        }

        /// <summary>
        /// Creates non-owned buffer.
        /// Calling Return is still valid, but effectively no-op.
        /// </summary>
        public static DataBuffer Create(byte[] buffer, int length)
        {
            return new DataBuffer(buffer, length, false);
        }

        public void Return()
        {
            if (_owned && _buffer != null)
            {
                s_pool.Return(_buffer);
                _buffer = null;
                _length = 0;
            }
        }
    }
}
