using System.Buffers;
using System.Runtime.CompilerServices;

namespace Glacie.Data.Compression.Utilities
{
    internal static class SharedBufferPool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Rent(int minimumLength)
        {
            return ArrayPool<byte>.Shared.Rent(minimumLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
