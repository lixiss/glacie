using System.Runtime.CompilerServices;

namespace Glacie.Data.Arz
{
    // TODO: (Low) (ArzFieldDataCodec) ArzFieldDataCodec is again helper method, which should be renamed / removed.

    internal static class ArzFieldDataCodec
    {
        public static void ValidateFieldDataSize(byte[] data)
        {
            var size = data.Length;

            if (size < 12)
            {
                throw Error.InvalidOperation("Field block has invalid size.");
            }

            if (size % 4 != 0)
            {
                throw Error.InvalidOperation("Field block has invalid size.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateFieldType(ArzValueType value)
        {
            if (value > ArzValueType.Boolean) throw Error.InvalidOperation("Invalid field type.");
        }
    }
}
