namespace Glacie
{
    // TODO: (High) (Variant) Rename members to better reflect data type. Integer -> Int32. Real -> Float32, etc.

    public enum VariantType : byte
    {
        // 0 is reserved for handling default-initialized value.
        //Null = 0,

        Integer = 1,
        Real = 2,
        Boolean = 3,

        // Values below should be reference-type values.

        String = 4,
        IntegerArray = 5,
        RealArray = 6,
        BooleanArray = 7,
        StringArray = 8,

        // TODO: Make Float64 support optional.
        Float64Array = 9,
    }
}
