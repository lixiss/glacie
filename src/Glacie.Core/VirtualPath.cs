using System;

namespace Glacie
{
    public readonly struct VirtualPath : IEquatable<VirtualPath>, IEquatable<string>
    {
        public const char DirectorySeparatorChar = '/';
        public const char AltDirectorySeparatorChar = '\\';

        private readonly string _value;
        private readonly Flags _flags;

        public VirtualPath(string value)
        {
            _value = value;
            _flags = 0;
        }

        private VirtualPath(string value, Flags flags)
        {
            _value = value;
            _flags = flags;
        }

        public string Value => _value;

        public VirtualPath Normalize(VirtualPathNormalization normalization)
        {
            var normalizationMasked = (Flags)normalization & Flags.NormalizationMask;
            if ((_flags & Flags.NormalizationMask)
                == (normalizationMasked | Flags.Normalized))
            {
                // Value already normalized.
                return this;
            }

            if (string.IsNullOrEmpty(_value)) return this;

            // TODO: (High) (VritualPath) Perform actual path normalization.

            var value = _value;

            if ((normalizationMasked & Flags.DirectorySeparator) != 0)
            {
                value = value.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);
            }

            if ((normalizationMasked & Flags.LowerInvariant) != 0)
            {
                value = value.ToLowerInvariant();
            }

            var flags = _flags | Flags.Normalized | normalizationMasked;

            return new VirtualPath(value, flags);
        }

        // TODO: Check valid characters
        // TODO: Not rooted

        #region Equality

        public bool Equals(in VirtualPath other)
        {
            return StringComparer.Ordinal.Equals(_value, other._value);
        }

        public bool Equals(in VirtualPath other, VirtualPathComparison comparison = VirtualPathComparison.Ordinal)
        {
            return Equals(_value.AsSpan(), _flags, other._value.AsSpan(), other._flags, comparison);
        }

        private static bool Equals(in ReadOnlySpan<char> value, Flags valueFlags, in ReadOnlySpan<char> other, Flags otherFlags, VirtualPathComparison comparison)
        {
            switch (comparison)
            {
                case VirtualPathComparison.Ordinal:
                    return value.Equals(other, StringComparison.Ordinal);

                case VirtualPathComparison.OrdinalIgnoreCase:
                    if ((valueFlags & Flags.LowerInvariant) != 0
                        && (otherFlags & Flags.LowerInvariant) != 0)
                    {
                        return value.Equals(other, StringComparison.Ordinal);
                    }
                    else return value.Equals(other, StringComparison.OrdinalIgnoreCase);

                case VirtualPathComparison.OrdinalIgnoreDirectorySeparator:
                    if ((valueFlags & Flags.DirectorySeparator) != 0
                        && (otherFlags & Flags.DirectorySeparator) != 0)
                    {
                        return value.Equals(other, StringComparison.Ordinal);
                    }
                    return EqualsIgnoreDirectorySeparator(value, other);

                case VirtualPathComparison.OrdinalIgnoreCaseAndDirectorySeparator:
                    {
                        var mask = Flags.DirectorySeparator | Flags.LowerInvariant;
                        if ((valueFlags & mask) == mask && (otherFlags & mask) == mask)
                        {
                            return value.Equals(other, StringComparison.Ordinal);
                        }
                        return EqualsIgnoreCaseAndDirectorySeparator(value, other);
                    }

                default:
                    throw Error.Argument(nameof(comparison));
            }
        }

        private static bool EqualsIgnoreDirectorySeparator(string? a, string? b)
        {
            if ((object?)a == b) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            return EqualsIgnoreDirectorySeparator(a.AsSpan(), b.AsSpan());
        }

        private static bool EqualsIgnoreDirectorySeparator(in ReadOnlySpan<char> a, in ReadOnlySpan<char> b)
        {
            if (a.Length != b.Length) return false;

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    if (a[i] == DirectorySeparatorChar && b[i] == AltDirectorySeparatorChar)
                    {
                        // no-op
                    }
                    else if (a[i] == AltDirectorySeparatorChar && b[i] == DirectorySeparatorChar)
                    {
                        // no-op
                    }
                    else return false;
                }
            }
            return true;
        }

        private static bool EqualsIgnoreCaseAndDirectorySeparator(string? a, string? b)
        {
            if ((object?)a == b) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            return EqualsIgnoreCaseAndDirectorySeparator(a.AsSpan(), b.AsSpan());
        }

        private static bool EqualsIgnoreCaseAndDirectorySeparator(in ReadOnlySpan<char> a, in ReadOnlySpan<char> b)
        {
            if (a.Length != b.Length) return false;

            for (var i = 0; i < a.Length; i++)
            {
                if (char.ToLowerInvariant(a[i]) != char.ToLowerInvariant(b[i]))
                {
                    if (a[i] == DirectorySeparatorChar && b[i] == AltDirectorySeparatorChar)
                    {
                        // no-op
                    }
                    else if (a[i] == AltDirectorySeparatorChar && b[i] == DirectorySeparatorChar)
                    {
                        // no-op
                    }
                    else return false;
                }
            }
            return true;
        }

        #endregion

        public bool StartsWith(in VirtualPath segment, VirtualPathComparison comparison)
        {
            var sValue = segment._value;
            if (sValue.Length > _value.Length) return false;
            return Equals(_value.AsSpan(0, sValue.Length), _flags, segment._value, segment._flags, comparison);
        }

        public bool StartsWithSegment(in VirtualPath segment, VirtualPathComparison comparison)
        {
            var sValue = segment._value;
            if (sValue.Length > _value.Length) return false;

            if (Equals(_value.AsSpan(0, sValue.Length), _flags, segment._value, segment._flags, comparison))
            {
                if (IsDirectorySeparatorChar(sValue[sValue.Length - 1]))
                {
                    return true;
                }

                if (_value.Length > segment._value.Length)
                {
                    var i = segment._value.Length;
                    return IsDirectorySeparatorChar(_value[i]);
                }

                return true;
            }
            return false;
        }

        public VirtualPath TrimStart(string value, VirtualPathComparison comparison)
        {
            if (StartsWith(value, comparison))
            {
                return new VirtualPath(_value.Substring(value.Length), _flags);
            }
            return this;
        }

        public VirtualPath TrimStartSegment(in VirtualPath segment, VirtualPathComparison comparison)
        {
            if (string.IsNullOrEmpty(segment._value)) return this;

            if (StartsWithSegment(segment, comparison))
            {
                var i = segment._value.Length;
                while (i < _value.Length && IsDirectorySeparatorChar(_value[i]))
                {
                    i++;
                }
                return new VirtualPath(i < _value.Length ? _value.Substring(i) : "", _flags);
            }
            return this;
        }

        public static VirtualPath Combine(in VirtualPath p1, in VirtualPath p2)
        {
            // TODO: (High) (VirtualPath) Combine
            return new VirtualPath(System.IO.Path.Combine(p1.Value, p2.Value));
        }

        #region Object

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value;

        public override bool Equals(object obj)
        {
            if (obj is VirtualPath other)
                return Equals(in other);
            return false;
        }

        // TODO: equality operators

        #endregion

        #region IEquatable

        public bool Equals(VirtualPath other)
        {
            return StringComparer.Ordinal.Equals(_value, other._value);
        }

        public bool Equals(string other)
        {
            return StringComparer.Ordinal.Equals(_value, other);
        }

        #endregion

        public static implicit operator string(in VirtualPath value) => value._value;

        public static implicit operator VirtualPath(string value) => new VirtualPath(value);

        private static bool IsDirectorySeparatorChar(char ch)
        {
            return ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar;
        }

        [Flags]
        private enum Flags : int
        {
            // Keep values in sync with VirtualPathNormalization.
            DirectorySeparator = 1 << 0,
            LowerInvariant = 1 << 1,
            NormalizationMask = DirectorySeparator | LowerInvariant,

            Normalized = 1 << 2,

            None = 0,
        }
    }
}
