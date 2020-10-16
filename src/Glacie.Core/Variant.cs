using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Glacie.Data.Arz.Infrastructure;

namespace Glacie
{
    // Variant-like type which can hold different types of values.
    // Simple values, like int, float and bool are stored as encoded raw value (int32).
    // Doubles are converted to float.
    // Other supported values are reference values, and stored as is.

    // TODO: (Medium) Redesign use of internal Variant members (we generally should not need them).
    // TODO: (Medium) Complete Variant API. (equality comparisons, ToString, HashCode, To<T>, etc...).

    public readonly struct Variant
    {
        private readonly VariantType _type;
        private readonly int _value;
        private readonly object? _refValue;

        [Obsolete("Limit use of internal ctor.")]
        internal Variant(VariantType type, int rawValue, object? refValue)
        {
            DebugCheck.True(
                (type < VariantType.String && refValue == null)
                || (type >= VariantType.String && refValue != null)
                );

            _type = type;
            _value = rawValue;
            _refValue = refValue;
        }

        private Variant(int value)
        {
            _type = VariantType.Integer;
            _value = value;
            _refValue = null;
        }

        private Variant(float value)
        {
            _type = VariantType.Real;
            _value = ArzBitConverter.Float32ToInt32(value);
            _refValue = null;
        }

        private Variant(bool value)
        {
            _type = VariantType.Boolean;
            _value = value ? 1 : 0;
            _refValue = null;
        }

        private Variant(string value)
        {
            _type = VariantType.String;
            _value = 0;
            _refValue = value;
        }

        public VariantType Type => _type;

        // TOOD: Rename to Arity. Also rename ArzField.Arity and Field (when exist) .Arity.
        public int Count => _refValue == null ? 1 : GetCountInternal();

        // TODO: Get<T> - get by underlying type?
        // TODO: As<T> - without conversion...
        // TODO: To<T> - perform conversions? how it is differs?

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is<T>() => IsImpl<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>() => GetUnderlyingValue<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(int index) => GetValueAt<T>(index);

        [Obsolete("Not yet implemented.", true)]
        public T To<T>() => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public T As<T>() => throw Error.NotImplemented();

        // TODO: This is exactly Is<T> method. Stop use useless forwarding.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsImpl<T>()
        {
            if (typeof(T) == typeof(float))
            {
                return _type == VariantType.Real;
            }
            else if (typeof(T) == typeof(bool))
            {
                return _type == VariantType.Boolean;
            }
            else if (typeof(T) == typeof(int))
            {
                return _type == VariantType.Integer;
            }
            else if (typeof(T) == typeof(string))
            {
                return _type == VariantType.String;
            }
            else if (typeof(T) == typeof(float[]))
            {
                return _type == VariantType.RealArray;
            }
            else if (typeof(T) == typeof(bool[]))
            {
                return _type == VariantType.BooleanArray;
            }
            else if (typeof(T) == typeof(int[]))
            {
                return _type == VariantType.IntegerArray;
            }
            else if (typeof(T) == typeof(string[]))
            {
                return _type == VariantType.StringArray;
            }
            else if (typeof(T) == typeof(double[]))
            {
                return _type == VariantType.Float64Array;
            }
            else throw Error.InvalidOperation();
        }

        private T GetUnderlyingValue<T>()
        {
            if (typeof(T) == typeof(float))
            {
                return (T)(object)GetReal();
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)GetBoolean();
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)GetInteger();
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)GetString();
            }
            else if (typeof(T) == typeof(float[]))
            {
                return (T)(object)GetRealArray();
            }
            else if (typeof(T) == typeof(bool[]))
            {
                return (T)(object)GetBooleanArray();
            }
            else if (typeof(T) == typeof(int[]))
            {
                return (T)(object)GetIntegerArray();
            }
            else if (typeof(T) == typeof(string[]))
            {
                return (T)(object)GetStringArray();
            }
            else if (typeof(T) == typeof(double[]))
            {
                return (T)(object)GetFloat64Array();
            }
            else throw Error.InvalidOperation("Unsupported type.");
        }

        private T GetElementValue<T>()
        {
            if (typeof(T) == typeof(float))
            {
                return (T)(object)GetReal();
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)GetBoolean();
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)GetInteger();
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)GetString();
            }
            else throw Error.InvalidOperation("Unsupported type.");
        }

        private T GetValueAt<T>(int index)
        {
            if (_refValue == null)
            {
                if (index != 0) throw Error.IndexOutOfRange();
                return GetElementValue<T>();
            }

            if (typeof(T) == typeof(float))
            {
                return (T)(object)GetRealAt(index);
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)GetBooleanAt(index);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)GetIntegerAt(index);
            }
            else if (typeof(T) == typeof(string))
            {
                if (_type == VariantType.String)
                {
                    if (index != 0) throw Error.IndexOutOfRange();
                    return (T)(object)GetString();
                }
                else
                {
                    return (T)(object)GetStringAt(index);
                }
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)GetFloat64At(index);
            }
            else throw Error.InvalidOperation("Unsupported type.");
        }

        private float GetReal()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.Real)
            {
                return ArzBitConverter.Int32ToFloat32(_value);
            }

            ThrowInvalidOperation();
            return 0;
        }

        private bool GetBoolean()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.Boolean)
            {
                return _value != 0;
            }

            ThrowInvalidOperation();
            return false;
        }

        private int GetInteger()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.Integer)
            {
                return _value;
            }

            ThrowInvalidOperation();
            return 0;
        }

        private string GetString()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.String)
            {
                return (string)_refValue!;
            }

            ThrowInvalidOperation();
            return null!;
        }

        private float[] GetRealArray()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.RealArray)
            {
                return (float[])_refValue!;
            }

            ThrowInvalidOperation();
            return null;
        }

        private double[] GetFloat64Array()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.Float64Array)
            {
                return (double[])_refValue!;
            }

            ThrowInvalidOperation();
            return null;
        }

        private bool[] GetBooleanArray()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.BooleanArray)
            {
                return (bool[])_refValue!;
            }

            ThrowInvalidOperation();
            return null;
        }

        private int[] GetIntegerArray()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.IntegerArray)
            {
                return (int[])_refValue!;
            }

            ThrowInvalidOperation();
            return null;
        }

        private string[] GetStringArray()
        {
            // TODO: (Low) (Variant) Try reverse without return 0 or just use normal throw. Might need measures.

            if (_type == VariantType.StringArray)
            {
                return (string[])_refValue!;
            }

            ThrowInvalidOperation();
            return null!;
        }

        private float GetRealAt(int index)
        {
            if (_type != VariantType.RealArray) throw Error.InvalidOperation();
            var values = (float[])_refValue!;
            return values[index];
        }

        private double GetFloat64At(int index)
        {
            if (_type != VariantType.Float64Array) throw Error.InvalidOperation();
            var values = (double[])_refValue!;
            return values[index];
        }

        private bool GetBooleanAt(int index)
        {
            if (_type != VariantType.BooleanArray) throw Error.InvalidOperation();
            var values = (bool[])_refValue!;
            return values[index];
        }

        private int GetIntegerAt(int index)
        {
            if (_type != VariantType.IntegerArray) throw Error.InvalidOperation();
            var values = (int[])_refValue!;
            return values[index];
        }

        private string GetStringAt(int index)
        {
            if (_type != VariantType.StringArray) throw Error.InvalidOperation();
            var values = (string[])_refValue!;
            return values[index];
        }

        private int GetCountInternal()
        {
            // TODO: (Low) (Variant) We might include all values into switch.
            switch (_type)
            {
                //case ArzVariantType.Integer:
                //case ArzVariantType.Real:
                //case ArzVariantType.Boolean:
                case VariantType.String:
                    return 1;

                case VariantType.RealArray:
                    return ((float[])_refValue!).Length;

                case VariantType.IntegerArray:
                    return ((int[])_refValue!).Length;

                case VariantType.StringArray:
                    return ((string[])_refValue!).Length;

                case VariantType.BooleanArray:
                    return ((bool[])_refValue!).Length;

                case VariantType.Float64Array:
                    return ((double[])_refValue!).Length;

                default: throw Error.Unreachable();
            }
        }

        [DoesNotReturn]
        private static void ThrowInvalidOperation()
        {
            throw Error.InvalidOperation();
        }

        public static implicit operator Variant(int value) => new Variant(value);
        public static implicit operator Variant(float value) => new Variant(value);
        public static implicit operator Variant(bool value) => new Variant(value);
        public static implicit operator Variant(string value) => new Variant(value);
        public static implicit operator Variant(double value) => new Variant(ArzBitConverter.Float64ToFloat32(value));

        // TODO: (Low) (Variant) (Decision) We can drop array for single valued arrays. However this already handled in ArzRecord.
        public static implicit operator Variant(int[] values) => new Variant(VariantType.IntegerArray, 0, values);
        public static implicit operator Variant(float[] values) => new Variant(VariantType.RealArray, 0, values);
        public static implicit operator Variant(bool[] values) => new Variant(VariantType.BooleanArray, 0, values);
        public static implicit operator Variant(string[] values) => new Variant(VariantType.StringArray, 0, values);
        public static implicit operator Variant(double[] values) => new Variant(VariantType.Float64Array, 0, values);

        // TODO: (Low) (Decision) Explicit casting is same as Get<int>(): may be rename Get to GetUnderlyingValue<T>()?
        public static explicit operator int(Variant value) => value.Get<int>();
        public static explicit operator float(Variant value) => value.Get<float>();
        public static explicit operator double(Variant value) => value.Get<double>();
        public static explicit operator bool(Variant value) => value.Get<bool>();
        public static explicit operator string(Variant value) => value.Get<string>();

        public static explicit operator int[](Variant value) => value.Get<int[]>();
        public static explicit operator float[](Variant value) => value.Get<float[]>();
        public static explicit operator double[](Variant value) => value.Get<double[]>();
        public static explicit operator bool[](Variant value) => value.Get<bool[]>();
        public static explicit operator string[](Variant value) => value.Get<string[]>();
    }
}
