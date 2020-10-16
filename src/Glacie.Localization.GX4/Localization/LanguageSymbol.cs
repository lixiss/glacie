using System;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Localization
{
    /// <summary>
    /// Identifies language, maps to <see cref="Localization.LanguageInfo"/>.
    /// </summary>
    public readonly struct LanguageSymbol : IEquatable<LanguageSymbol>
    {
        public static LanguageSymbol Invariant => default;
        public static LanguageSymbol English => new LanguageSymbol(1);

        internal readonly short _ordinal;

        internal LanguageSymbol(short ordinal)
        {
            _ordinal = ordinal;
        }

        public LanguageInfo LanguageInfo => LanguageRegistry.GetLanguageInfo(_ordinal);

        public override string ToString()
        {
            return LanguageInfo.Name;
        }

        #region Equality

        public override int GetHashCode()
        {
            return _ordinal;
        }

        public override bool Equals(object? obj)
        {
            if (obj is LanguageSymbol x) return Equals(x);
            return false;
        }

        public bool Equals([AllowNull] LanguageSymbol other)
        {
            return _ordinal == other._ordinal;
        }

        public static bool operator ==(LanguageSymbol x, LanguageSymbol y)
        {
            return x._ordinal == y._ordinal;
        }

        public static bool operator !=(LanguageSymbol x, LanguageSymbol y)
        {
            return x._ordinal != y._ordinal;
        }

        #endregion
    }
}
