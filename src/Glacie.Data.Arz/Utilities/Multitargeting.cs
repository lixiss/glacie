using System.Runtime.CompilerServices;

namespace Glacie.Data.Arz
{
    // TODO: (VeryLow) (Multitargeting) (AllocateUninitializedByteArray) - similar classes exist in other projects. Rename class, or don't use it. Or move to Glacie.Private.Core.

    internal static class Multitargeting
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AllocateUninitializedByteArray(int length)
        {
#if NET5_0
            return GC.AllocateUninitializedArray<byte>(length);
#else
            return new byte[length];
#endif
        }
    }
}
