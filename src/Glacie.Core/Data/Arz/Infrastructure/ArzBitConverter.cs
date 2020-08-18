using System;
using System.Runtime.CompilerServices;

// TODO: (Low) (ArzBitConverter) Move to some another namespace.
// Generally this type is not intended to use directly.

namespace Glacie.Data.Arz.Infrastructure
{
    public static class ArzBitConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float Int32ToFloat32(int value)
        {
            return *(float*)&value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Float32ToInt32(float value)
        {
            return *(int*)&value;
        }

        public static void CheckFinite(float value)
        {
            if (!float.IsFinite(value)) throw ArzError.NotFiniteNumber();
        }

        public static void CheckFinite(double value)
        {
            if (!double.IsFinite(value)) throw ArzError.NotFiniteNumber();
        }

        public static int Float64ToInt32(double value)
        {
            return Float32ToInt32(Float64ToFloat32(value));
        }

        public static float Float64ToFloat32(double value)
        {
            if (Features.ThrowOnArithmeticOverflow)
            {
                if (Features.Float64RoundTripConversionCheck)
                {
                    float result = (float)value;
                    double residual = value - (double)result;
                    if (Math.Abs(residual) >= 1.0) throw ArzError.ArithmeticOverflow();

                    return result;
                }
                else
                {
                    const double validMinInclusive = -16777216.0;
                    const double validMaxInclusive = 16777216.0;

                    if (validMinInclusive <= value && value <= validMaxInclusive)
                        return (float)value;
                    else if (double.IsFinite(value))
                        throw ArzError.ArithmeticOverflow();
                    else
                    {
                        if (Features.ThrowOnNonFiniteValues)
                        {
                            throw ArzError.NotFiniteNumber();
                        }
                        else return (float)value;
                    }
                }
            }
            else
            {
                return (float)value;
            }
        }
    }
}
