namespace Glacie.Data.Arz
{
    // TODO: (Low) (Features) Features should be defined by compile-time constants,
    // and constants should be mapped to this type.

    public static class Features
    {
        public static bool RecordMapEnabled => true;

        public static bool ThrowOnNonFiniteValues => true;

        public static bool ThrowOnArithmeticOverflow => true;

        public static bool Float64RoundTripConversionCheck => true;
    }
}
