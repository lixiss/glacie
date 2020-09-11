using System;

namespace Glacie.Data
{
    // TODO: aggressive inlining

    internal readonly struct GXGameType : IEquatable<GXGameType>
    {
        private const int VALUE_TQ = 1;
        private const int VALUE_TQIT = 2;
        private const int VALUE_TQAE = 3;
        private const int VALUE_GD = 4;

        private const string STRING_TQ = "TQ";
        private const string STRING_TQIT = "TQIT";
        private const string STRING_TQAE = "TQAE";
        private const string STRING_GD = "GD";
        private const string STRING_UNKNOWN = "UNKNOWN";

        #region Static

        public static readonly GXGameType TitanQuest = new GXGameType(VALUE_TQ);
        public static readonly GXGameType TitanQuestImmortalThrone = new GXGameType(VALUE_TQIT);
        public static readonly GXGameType TitanQuestAnniversaryEdition = new GXGameType(VALUE_TQAE);
        public static readonly GXGameType GrimDawn = new GXGameType(VALUE_GD);

        public static bool TryParse(string? value, out GXGameType result)
        {
            if (value == null)
            {
                result = default;
                return false;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;

            if (comparer.Equals(value, STRING_TQ))
            {
                result = TitanQuest;
                return true;
            }
            else if (comparer.Equals(value, STRING_TQIT))
            {
                result = TitanQuestImmortalThrone;
                return true;
            }
            else if (comparer.Equals(value, STRING_TQAE))
            {
                result = TitanQuestAnniversaryEdition;
                return true;
            }
            else if (comparer.Equals(value, STRING_GD))
            {
                result = GrimDawn;
                return true;
            }
            else throw Error.Argument(nameof(value));
        }

        public static GXGameType Parse(string? value)
        {
            if (TryParse(value, out var result)) return result;
            throw Error.Argument(nameof(value));
        }

        public static string ToString(GXGameType value)
        {
            return value.ToString();
        }

        #endregion

        private readonly int _value;

        private GXGameType(int value)
        {
            _value = value;
        }

        #region Equatable

        public override string ToString()
        {
            return _value switch
            {
                VALUE_TQ => STRING_TQ,
                VALUE_TQIT => STRING_TQIT,
                VALUE_TQAE => STRING_TQAE,
                VALUE_GD => STRING_GD,
                _ => STRING_UNKNOWN,
            };
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is GXGameType other)
            {
                return _value == other._value;
            }
            return false;
        }

        public bool Equals(GXGameType other)
        {
            return _value == other._value;
        }

        public static bool operator ==(GXGameType a, GXGameType b)
        {
            return a._value == b._value;
        }

        public static bool operator !=(GXGameType a, GXGameType b)
        {
            return a._value != b._value;
        }

        #endregion
    }
}
