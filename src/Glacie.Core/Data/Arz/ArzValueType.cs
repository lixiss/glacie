namespace Glacie.Data.Arz
{
    // TODO: (Low) (Decision) In order if ArzValueType can be used outside Glacie.Data.Arz,
    // then we need rename this type (something DatabaseValueType or FieldValueType).
    // Alternatively: Move ArzValueType back into Glacie.Data.Arz, and introduce
    // another enum, which can be just casted.

    // TODO: (Medium) ArzValueType: rename members to better reflect value type. Integer -> Int32. Real -> Float32.

    public enum ArzValueType : ushort
    {
        Integer = 0,
        Real = 1,
        String = 2,
        Boolean = 3,
    }
}
