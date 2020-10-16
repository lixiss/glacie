using System;
using System.Runtime.CompilerServices;

namespace Glacie
{
    /// <summary>
    /// Represents virtual path.
    /// </summary>
    //public static partial class Path
    public readonly partial struct Path : IEquatable<Path>, IEquatable<string>
    {
        public const char DirectorySeparatorChar = PathInternal.DirectorySeparatorChar;
        public const char AltDirectorySeparatorChar = PathInternal.AltDirectorySeparatorChar;
        public const char PreferredDirectorySeparatorChar = DirectorySeparatorChar;

        private readonly string? _value;

        public Path(string? value)
        {
            _value = value;
        }

        public bool HasValue => _value != null;

        public string? Value => _value;

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public override string ToString()
        {
            return _value ?? "";
        }

        #region Equality

        public bool Equals(Path path)
        {
            return PathInternal.EqualsOrdinal(_value, path._value);
        }

        public bool Equals(Path path, PathComparison comparison)
        {
            return PathInternal.Equals(_value, path._value, comparison);
        }

        public bool Equals(string path)
        {
            return PathInternal.EqualsOrdinal(_value, path);
        }

        public bool Equals(string path, PathComparison comparison)
        {
            return PathInternal.Equals(_value, path, comparison);
        }

        public override bool Equals(object obj)
        {
            if (obj is Path p) return Equals(p);
            else if (obj is string s) return Equals(s);
            return false;
        }

        #endregion

        #region Hashing

        public override int GetHashCode()
        {
            return PathInternal.GetHashCodeOrdinal(_value);
        }

        public int GetHashCode(PathComparison comparison)
        {
            switch (comparison)
            {
                case PathComparison.Ordinal:
                    return PathInternal.GetHashCodeOrdinal(_value);

                case PathComparison.OrdinalIgnoreCase:
                    return PathInternal.GetHashCodeOrdinalIgnoreCase(_value);

                default:
                    throw Error.Argument(nameof(comparison));
            }
        }

        #endregion

        #region Convert

        public Path Convert(PathConversions conversions)
        {
            // TODO: (Medium) (Path) Normalization of rooted paths happens strangely, see NormalizeRooted tests.

            var value = PathInternal.Convert(_value, conversions, out var outputConversions);
            return new Path(value);
        }
        public Path ConvertNonEmpty(PathConversions conversions)
        {
            if (IsEmpty) return this;
            // TODO: (Medium) (Path) Normalization of rooted paths happens strangely, see NormalizeRooted tests.

            var value = PathInternal.Convert(_value, conversions, out var outputConversions);
            return new Path(value);
        }

        public Path Convert(PathConversions conversions, bool check)
        {
            var value = PathInternal.Convert(_value, conversions, out var outputConversions);
            if (check && (outputConversions & conversions) != conversions)
            {
                throw Error.Argument(nameof(value), "Given path \"{0}\" has not been converted to given form \"{1}\".", _value, conversions);
            }
            return new Path(value);
        }

        public Path ConvertNonEmpty(PathConversions conversions, bool check)
        {
            if (IsEmpty) return this;

            var value = PathInternal.Convert(_value, conversions, out var outputConversions);
            if (check && (outputConversions & conversions) != conversions)
            {
                throw Error.Argument(nameof(value), "Given path \"{0}\" has not been converted to given form \"{1}\".", _value, conversions);
            }
            return new Path(value);
        }

        public bool TryConvert(PathConversions conversions, out Path result)
        {
            // TODO: (Low) (Path) This call can don't materialize string when not needed.
            var resultValue = PathInternal.Convert(_value, conversions, out var resultForm);
            if ((resultForm & conversions) == conversions)
            {
                result = new Path(resultValue);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }


        #endregion

        #region StartsWith

        public bool StartsWith(Path path, PathComparison comparison)
            => StartsWith(_value, path._value, comparison);

        public bool StartsWith(string path, PathComparison comparison)
            => StartsWith(_value, path, comparison);

        #endregion

        #region EndsWith

        [Obsolete("Warning: this method is not properly implemented if path value is multi-segment. But works for simple file name.")]
        public bool EndsWith(Path path, PathComparison comparison)
            => EndsWith(_value, path._value, comparison);

        [Obsolete("Warning: this method is not properly implemented if path value is multi-segment. But works for simple file name.")]
        public bool EndsWith(string path, PathComparison comparison)
            => EndsWith(_value, path, comparison);

        #endregion

        #region TrimStart

        public Path TrimStart(Path path, PathComparison comparison)
        {
            var index = PathInternal.StartsWith(_value.AsSpan(), path._value.AsSpan(), comparison);
            if (index >= 0)
            {
                var s = _value.AsSpan().Slice(index);
                var i = 0;
                while (i < s.Length && IsDirectorySeparator(s[i])) i++;
                var resultPath = s.Slice(i).ToString();

                return new Path(resultPath);
            }
            return this;
        }

        public bool TryTrimStart(Path path, PathComparison comparison, out Path result)
        {
            result = TrimStart(path, comparison);
            return (object?)path.Value != result.Value;
        }

        #endregion

        #region Validation

        public bool Validate(PathValidations options)
            => Validate(_value.AsSpan(), options);

        public bool Validate(PathValidations options, out PathValidations result)
            => Validate(_value.AsSpan(), options, out result);

        #endregion

        #region Temprorary

        [Obsolete("This is temporary API call. Thinking about making Path implicitly convertible from string.")]
        public static Path Implicit(string? value)
        {
            return new Path(value);
        }

        #endregion

        #region Decomposition

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetExtension() => GetExtension(_value);

        #endregion


        // may be implicit conversions with strings.

        /*
                public static bool Validate(string path, PathValidationOptions options)
                {
                    throw Error.InvalidOperation();
                }



                public static bool CheckInvalidCharacters(string path)
                {
                    return CheckInvalidCharacters(path.AsSpan());
                }

                public static bool CheckInvalidCharacters(ReadOnlySpan<char> path)
                {
                    // return CheckInvalidCharacters(ref MemoryMarshal.GetReference(path), path.Length);

                    for (var i = 0; i < path.Length; i++)
                    {
                        var ch = path[i];

                        // if (Array.IndexOf(s_invalidPathChars, ch) >= 0) return false;
                        // if (Array.BinarySearch(s_invalidPathChars, ch) >= 0) return false;

                        // if (ch < 32) return false;
                        if (IsInvalidPathOrFileNameChar(ch)) return false;

                        // if (ch < 32 || ch == '|') return false;

                        // if (s_windowsInvalidPathChars.Contains(ch)) return false;
                        // if (ArrayContains(s_windowsInvalidPathChars, ch)) return false;
                        // if (Array.BinarySearch(s_windowsInvalidPathChars, ch) >= 0) return false;
                    }
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool IsInvalidPathOrFileNameChar(char ch)
                {
                    if (ch <= 0x3F)
                    {
                        if (ch <= 0x2F)
                        {
                            return ch < 0x20
                                || ch == (char)0x22 // '"'
                                || ch == (char)0x2A // '*'
                                ;
                        }
                        else
                        {
                            return ch == (char)0x3A // ':'
                                || ch == (char)0x3C // '<'
                                || ch == (char)0x3E // '>'
                                || ch == (char)0x3F // '?'
                                ;
                        }
                    }
                    else return ch == (char)0x7C;
                }

                #region Invalid chars

                private static readonly char[] s_invalidPathChars = new char[]
                {
                    (char)0x00, (char)0x01, (char)0x02, (char)0x03, (char)0x04, (char)0x05, (char)0x06, (char)0x07,
                    (char)0x08, (char)0x09, (char)0x0A, (char)0x0B, (char)0x0C, (char)0x0D, (char)0x0E, (char)0x0F,
                    (char)0x10, (char)0x11, (char)0x12, (char)0x13, (char)0x14, (char)0x15, (char)0x16, (char)0x17,
                    (char)0x18, (char)0x19, (char)0x1A, (char)0x1B, (char)0x1C, (char)0x1D, (char)0x1E, (char)0x1F,
                    (char)0x22, // '"'
                    (char)0x2A, // '*'
                    // (char)0x2F, // '/'
                    (char)0x3A, // ':'
                    (char)0x3C, // '<'
                    (char)0x3E, // '>'
                    (char)0x3F, // '?'
                    // (char)0x5C, // '\\'
                    (char)0x7C, // '|'
                };

                private static readonly char[] s_invalidFileNameChars = new char[]
                {
                    (char)0x00, (char)0x01, (char)0x02, (char)0x03, (char)0x04, (char)0x05, (char)0x06, (char)0x07,
                    (char)0x08, (char)0x09, (char)0x0A, (char)0x0B, (char)0x0C, (char)0x0D, (char)0x0E, (char)0x0F,
                    (char)0x10, (char)0x11, (char)0x12, (char)0x13, (char)0x14, (char)0x15, (char)0x16, (char)0x17,
                    (char)0x18, (char)0x19, (char)0x1A, (char)0x1B, (char)0x1C, (char)0x1D, (char)0x1E, (char)0x1F,
                    (char)0x22, // '"'
                    (char)0x2A, // '*'
                    (char)0x2F, // '/'
                    (char)0x3A, // ':'
                    (char)0x3C, // '<'
                    (char)0x3E, // '>'
                    (char)0x3F, // '?'
                    (char)0x5C, // '\\'
                    (char)0x7C, // '|'
                };


                private static readonly char[] s_unixInvalidFileNameChars = new char[] { '\0', '/' };
                private static readonly char[] s_unixInvalidPathChars = new char[] { '\0' };

                private static readonly char[] s_windowsInvalidFileNameChars = new char[]
                {
                    (char)0x00, (char)0x01, (char)0x02, (char)0x03, (char)0x04, (char)0x05, (char)0x06, (char)0x07,
                    (char)0x08, (char)0x09, (char)0x0A, (char)0x0B, (char)0x0C, (char)0x0D, (char)0x0E, (char)0x0F,
                    (char)0x10, (char)0x11, (char)0x12, (char)0x13, (char)0x14, (char)0x15, (char)0x16, (char)0x17,
                    (char)0x18, (char)0x19, (char)0x1A, (char)0x1B, (char)0x1C, (char)0x1D, (char)0x1E, (char)0x1F,
                    (char)0x22, // '"'
                    (char)0x2A, // '*'
                    (char)0x2F, // '/'
                    (char)0x3A, // ':'
                    (char)0x3C, // '<'
                    (char)0x3E, // '>'
                    (char)0x3F, // '?'
                    (char)0x5C, // '\\'
                    (char)0x7C, // '|'
                };

                private static char[] s_windowsInvalidPathChars = new char[]
                {
                    (char)0x00, (char)0x01, (char)0x02, (char)0x03, (char)0x04, (char)0x05, (char)0x06, (char)0x07,
                    (char)0x08, (char)0x09, (char)0x0A, (char)0x0B, (char)0x0C, (char)0x0D, (char)0x0E, (char)0x0F,
                    (char)0x10, (char)0x11, (char)0x12, (char)0x13, (char)0x14, (char)0x15, (char)0x16, (char)0x17,
                    (char)0x18, (char)0x19, (char)0x1A, (char)0x1B, (char)0x1C, (char)0x1D, (char)0x1E, (char)0x1F,
                    (char)0x7C, // '|'
                };

                #endregion

                // Need method which will answer on some questions:
                // IsRelative
                // IsNormalized_NoRelativeSegments (e.g. has no relative segments)
                // IsNormalized_NoMultipleDirectorySeparators (e.g. zzz//is => is not normalized)
                // IsNormalized_PathUseSameDirectorySeparators
                // IsNormalized_IsDirectorySeparator
                // IsNormalized_IsAltDirectorySeparator
                // HasInvalidCharacters (segments should not have invalid characters)
                // IsInLowerInvariant
                // IsAscii

                */
    }
}
