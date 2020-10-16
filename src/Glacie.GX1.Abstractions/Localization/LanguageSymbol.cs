using System;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.GX1.Abstractions.Localization
{
    /// <summary>
    /// Identifies language, maps to LanguageInfo.
    /// </summary>
    public readonly struct LanguageSymbol : IEquatable<LanguageSymbol>
    {
        private readonly short _value;

        internal LanguageSymbol(short value)
        {
            _value = value;
        }

        public short Ordinal => _value;

        public LanguageInfo LanguageInfo => throw Error.NotImplemented();

        public override string ToString()
        {
            // TODO: get language info
            return LanguageInfo.IsoAlpha2Code;
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is LanguageSymbol x) return Equals(x);
            return false;
        }

        public bool Equals([AllowNull] LanguageSymbol other)
        {
            return _value == other._value;
        }
    }
}
