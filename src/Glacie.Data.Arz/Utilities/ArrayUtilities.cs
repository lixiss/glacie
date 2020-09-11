using System.Runtime.CompilerServices;

namespace Glacie.Data.Arz
{
    internal static class ArrayUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] AllocateUninitializedArray<T>(int length)
        {
#if NET5_0
            return GC.AllocateUninitializedArray<T>(length);
#else
            return new T[length];
#endif
        }
    }
}
