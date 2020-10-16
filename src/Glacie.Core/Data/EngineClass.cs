using System;

namespace Glacie.Data
{
    // TODO: Aggressive inlining

    public readonly struct EngineClass : IEquatable<EngineClass>
    {
        private const int VALUE_UNKNOWN = 0;
        private const int VALUE_CUSTOM = 1;
        private const int VALUE_UNIFIED = 2;
        private const int VALUE_TQ = 3;
        private const int VALUE_TQIT = 4;
        private const int VALUE_TQAE = 5;
        private const int VALUE_GD = 6;

        private const string STRING_UNKNOWN = "Unknown";
        private const string STRING_CUSTOM = "Custom";
        private const string STRING_UNIFIED = "Unified";
        private const string STRING_TQ = "TQ";
        private const string STRING_TQIT = "TQIT";
        private const string STRING_TQAE = "TQAE";
        private const string STRING_GD = "GD";

        #region Static

        public static readonly EngineClass Unknown = new EngineClass(VALUE_UNKNOWN);
        public static readonly EngineClass Custom = new EngineClass(VALUE_CUSTOM);
        public static readonly EngineClass Unified = new EngineClass(VALUE_UNIFIED);
        public static readonly EngineClass TQ = new EngineClass(VALUE_TQ);
        public static readonly EngineClass TQIT = new EngineClass(VALUE_TQIT);
        public static readonly EngineClass TQAE = new EngineClass(VALUE_TQAE);
        public static readonly EngineClass GD = new EngineClass(VALUE_GD);

        public static bool TryParse(string? value, out EngineClass result)
        {
            if (value == null)
            {
                result = default;
                return false;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;

            if (comparer.Equals(value, STRING_CUSTOM))
            {
                result = Custom;
                return true;
            }
            else if (comparer.Equals(value, STRING_UNIFIED))
            {
                result = Unified;
                return true;
            }
            else if (comparer.Equals(value, STRING_TQ))
            {
                result = TQ;
                return true;
            }
            else if (comparer.Equals(value, STRING_TQIT))
            {
                result = TQIT;
                return true;
            }
            else if (comparer.Equals(value, STRING_TQAE))
            {
                result = TQAE;
                return true;
            }
            else if (comparer.Equals(value, STRING_GD))
            {
                result = GD;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static EngineClass Parse(string? value)
        {
            if (TryParse(value, out var result)) return result;
            throw Error.Argument(nameof(value));
        }

        public static string ToString(EngineClass value)
        {
            return value.ToString();
        }

        #endregion

        private readonly int _value;

        private EngineClass(int value)
        {
            _value = value;
        }

        #region Equatable

        public bool Equals(EngineClass other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public override string ToString()
        {
            return _value switch
            {
                VALUE_CUSTOM => STRING_CUSTOM,
                VALUE_UNIFIED => STRING_UNIFIED,
                VALUE_TQ => STRING_TQ,
                VALUE_TQIT => STRING_TQIT,
                VALUE_TQAE => STRING_TQAE,
                VALUE_GD => STRING_GD,
                _ => STRING_UNKNOWN,
            };
        }

        public override bool Equals(object? obj)
        {
            if (obj is EngineClass other)
            {
                return _value == other._value;
            }
            return false;
        }

        public static bool operator ==(EngineClass a, EngineClass b)
        {
            return a._value == b._value;
        }

        public static bool operator !=(EngineClass a, EngineClass b)
        {
            return a._value != b._value;
        }

        #endregion
    }
}
