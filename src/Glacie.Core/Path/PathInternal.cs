using System;
using System.Runtime.CompilerServices;

using Glacie.Text;

namespace Glacie
{
    // TODO: Also more on file conventions, validate common file names too (e.g. like CON or LPT)?
    // See: https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file


    // Path = RootName (option) / RootDirectoryChar (optional) / RelativePath
    // Path = RootPath / RelativePath

    internal static class PathInternal
    {
        public const char DirectorySeparatorChar = '/';
        public const char AltDirectorySeparatorChar = '\\';

        private const char VolumeSeparatorChar = ':';
        private const int DevicePrefixLength = 4;      // \\?\, \\.\, \??\
        private const int UncPrefixLength = 2;         // \\
        private const int UncExtendedPrefixLength = 8; // \\?\UNC\, \\.\UNC\

        #region Conversion

        public static string Convert(string? path, PathConversions conversions, out PathConversions resultForm)
        {
            string? result;
            resultForm = PathConversions.None;

            var rootLength = GetRootLength(path);

            if ((conversions & PathConversions.Normalized) != 0)
            {
                const PathConversions supportedConversions =
                    PathConversions.Strict
                    | PathConversions.Normalized
                    | PathConversions.DirectorySeparator
                    | PathConversions.AltDirectorySeparator
                    | PathConversions.LowerInvariant;

                result = Normalize(path, rootLength, conversions & supportedConversions, out resultForm);
            }
            else
            {
                result = path;

                if ((conversions & PathConversions.DirectorySeparator) != 0)
                {
                    result = result?.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);

                    resultForm |= PathConversions.DirectorySeparator;
                }
                else if ((conversions & PathConversions.AltDirectorySeparator) != 0)
                {
                    result = result?.Replace(DirectorySeparatorChar, AltDirectorySeparatorChar);

                    resultForm |= PathConversions.AltDirectorySeparator;
                }

                if ((conversions & PathConversions.LowerInvariant) != 0)
                {
                    result = result?.ToLowerInvariant();

                    resultForm |= PathConversions.LowerInvariant;
                }
            }

            // Adjust path type bits
            {
                if (rootLength == 0)
                {
                    resultForm |= PathConversions.Relative;
                }
                else
                {
                    var absolute = PrefixEndsWithDirectorySeparator(result, rootLength);
                    resultForm |= absolute ? PathConversions.Absolute : PathConversions.Relative;
                    if (rootLength > 1)
                    {
                        resultForm |= PathConversions.Rooted;
                    }
                }
            }

            return result!;
        }

        private static string? Normalize(string? path, int rootLength, PathConversions conversions, out PathConversions resultForm)
        {
            // Normal form of an empty path is an empty path.
            if (string.IsNullOrEmpty(path))
            {
                resultForm = conversions;
                return path;
            }

            var sb = new ValueStringBuilder(stackalloc char[260]);

            if (Normalize(path.AsSpan(), rootLength, conversions, ref sb, out resultForm))
            {
                path = sb.ToString();
            }

            sb.Dispose();
            return path;
        }

        //private static bool Normalize(ReadOnlySpan<char> path,
        //    PathConversions conversions,
        //    ref ValueStringBuilder sb,
        //    out PathConversions resultForm)
        //{
        //    var rootLength = GetRootLength(path);
        //    return Normalize(path, rootLength, conversions, ref sb, out resultForm);
        //}

        private static bool PrefixEndsWithDirectorySeparator(ReadOnlySpan<char> path, int prefixLength)
        {
            return prefixLength > 0 && IsDirectorySeparator(path[prefixLength - 1]);
        }

        private static bool Normalize(ReadOnlySpan<char> path,
            int prefixLength,
            PathConversions form,
            ref ValueStringBuilder sb,
            out PathConversions resultForm)
        {
            // This method support flags:
            const PathConversions supportedConversions
                = PathConversions.Strict
                | PathConversions.Normalized
                | PathConversions.DirectorySeparator
                | PathConversions.AltDirectorySeparator
                | PathConversions.LowerInvariant;

            // Ensure what this method called only with supported flags,
            // and only with Normalized flag.
            DebugCheck.That((form & ~supportedConversions) == 0);
            DebugCheck.That((form & PathConversions.Normalized) != 0);

            var strict = (form & PathConversions.Strict) != 0;

            var modified = false;
            var hasNoRelativeSegments = true;

            var rootPrefixLength = prefixLength;

            var index = prefixLength;
            if (prefixLength > 0)
            {
                sb.Append(path.Slice(0, prefixLength));

                // Currently rooted path can be ends only with directory separator.
                // On windows there is other rule (c:some_path) is relative to current directory path.
                if (!IsDirectorySeparator(path[prefixLength - 1]))
                {
                    // TODO: Complete rooted paths support.
                    throw Error.NotImplemented("Working with rooted paths is not implemented properly.");
                }

                if (IsDirectorySeparator(path[prefixLength - 1]))
                {
                    // After root we can have multiple directory separator chars. Skip them.
                    // E.g. "//" => "/" or "/\\\\" -> "/" or "\\///" -> "\\".
                    if (index < path.Length && IsDirectorySeparator(path[index]))
                    {
                        index++;
                        while (index < path.Length && IsDirectorySeparator(path[index])) { index++; }

                        modified = true;
                    }
                }
            }

            // Process each segment.
            while (index < path.Length)
            {
                var segmentStartIndex = index;

                while (index < path.Length && !IsDirectorySeparator(path[index])) index++;
                var segmentLength = index - segmentStartIndex;
                if (segmentLength == 0) break;

                if (segmentLength <= 2 && path[segmentStartIndex] == '.')
                {
                    if (segmentLength == 2 && path[segmentStartIndex + 1] == '.')
                    {
                        // Trace back to parent segment.
                        if (sb.Length > prefixLength)
                        {
                            int ss = sb.Length - 1;
                            int s = ss;

                            if (s > prefixLength && IsDirectorySeparator(sb[s])) s--;
                            while (s > prefixLength && !IsDirectorySeparator(sb[s])) s--;

                            if (s <= prefixLength)
                            {
                                sb.Length = prefixLength;
                            }
                            else
                            {
                                sb.Length = s + 1;
                            }

                            if (index < path.Length)
                            {
                                while (index < path.Length && IsDirectorySeparator(path[index])) index++;
                                continue;
                            }
                            else
                            {
                                //// Trailing segment.
                                //if (sb.Length > rootLength && IsDirectorySeparator(sb[sb.Length - 1]))
                                //{
                                //    sb.Length--;
                                //}
                                break;
                            }
                        }
                        else // Parent directory segment points above prefix
                        {
                            if (!strict && PrefixEndsWithDirectorySeparator(path, prefixLength))
                            {
                                while (index < path.Length && IsDirectorySeparator(path[index])) index++;
                                continue;
                            }
                            else
                            {
                                // Emit parent segment as is.
                                hasNoRelativeSegments = false;

                                // Append segment.
                                sb.Append(path.Slice(segmentStartIndex, segmentLength));
                                prefixLength += segmentLength;

                                // Add directory separator to output.
                                if (index < path.Length)
                                {
                                    var c = path[index];
                                    index++;

                                    // Eat directory separators
                                    while (index < path.Length && IsDirectorySeparator(path[index])) index++;

                                    if (index < path.Length)
                                    {
                                        if ((form & PathConversions.DirectorySeparator) != 0)
                                        {
                                            modified |= c != DirectorySeparatorChar;
                                            c = DirectorySeparatorChar;
                                        }
                                        else if ((form & PathConversions.AltDirectorySeparator) != 0)
                                        {
                                            modified |= c != AltDirectorySeparatorChar;
                                            c = AltDirectorySeparatorChar;
                                        }

                                        sb.Append(c);
                                        prefixLength += 1;
                                    }

                                    DebugCheck.That(IsDirectorySeparator(c));
                                }
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // Current directory segment
                        if (index < path.Length)
                        {
                            // Eat next path separators.
                            while (index < path.Length && IsDirectorySeparator(path[index])) index++;
                            //sb.Append(path.Slice(segmentStartIndex, segmentLength));
                            continue;
                        }
                        else
                        {
                            // There is last segment, e.g. "a/." -> "a/".
                            //if (sb.Length > rootLength && IsDirectorySeparator(sb[sb.Length - 1]))
                            //{
                            //    sb.Length--;
                            //}
                            break;
                        }
                    }
                }


                // Append segment.
                if ((form & PathConversions.LowerInvariant) != 0)
                {
                    var segmentSpan = path.Slice(segmentStartIndex, segmentLength);
                    var i = 0;
                    while (i < segmentSpan.Length)
                    {
                        if ((uint)(segmentSpan[i] - 'A') <= ('Z' - 'A'))
                        {
                            break;
                        }
                        i++;
                    }

                    if (i == segmentSpan.Length)
                    {
                        sb.Append(segmentSpan);
                    }
                    else
                    {
                        var targetSpan = sb.AppendSpan(segmentSpan.Length);
                        if (i > 0)
                        {
                            segmentSpan.Slice(0, i).CopyTo(targetSpan.Slice(0, i));
                        }
                        segmentSpan.Slice(i, segmentSpan.Length - i)
                            .ToLowerInvariant(targetSpan.Slice(i, segmentSpan.Length - i));
                        modified = true;
                    }
                }
                else
                {
                    sb.Append(path.Slice(segmentStartIndex, segmentLength));
                }

                // Add directory separator to output.
                if (index < path.Length)
                {
                    var c = path[index];
                    index++;

                    if ((form & PathConversions.DirectorySeparator) != 0)
                    {
                        modified |= c != DirectorySeparatorChar;
                        c = DirectorySeparatorChar;
                    }
                    else if ((form & PathConversions.AltDirectorySeparator) != 0)
                    {
                        modified |= c != AltDirectorySeparatorChar;
                        c = AltDirectorySeparatorChar;
                    }

                    sb.Append(c);

                    DebugCheck.That(IsDirectorySeparator(c));

                    // Eat directory separators
                    while (index < path.Length && IsDirectorySeparator(path[index])) index++;
                }
            }

            if (sb.Length == rootPrefixLength
                && !PrefixEndsWithDirectorySeparator(path, prefixLength))
            {
                sb.Append('.');

                hasNoRelativeSegments = false;
            }

            // Calculate output form
            if (hasNoRelativeSegments)
            {
                resultForm = strict
                    ? PathConversions.Normalized | PathConversions.Strict
                    : PathConversions.Normalized;
            }
            else
            {
                resultForm = 0;
            }

            if ((form & PathConversions.DirectorySeparator) != 0)
            {
                resultForm |= PathConversions.DirectorySeparator;
            }
            else if ((form & PathConversions.AltDirectorySeparator) != 0)
            {
                resultForm |= PathConversions.AltDirectorySeparator;
            }

            if ((form & PathConversions.LowerInvariant) != 0)
            {
                resultForm |= PathConversions.LowerInvariant;
            }

            if (!modified && sb.Length == path.Length)
            {
                return false;
            }

            return true;
        }

        #endregion


        #region Equality

        public static bool EqualsOrdinal(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
        {
            return Equals(s1, s2, StringComparison.Ordinal);
        }

        public static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
        {
            return Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool Equals(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, PathComparison comparison)
        {
            switch (comparison)
            {
                case PathComparison.Ordinal:
                    return Equals(s1, s2, StringComparison.Ordinal);

                case PathComparison.OrdinalIgnoreCase:
                    return Equals(s1, s2, StringComparison.OrdinalIgnoreCase);

                default:
                    throw Error.Argument(nameof(comparison));
            }
        }

        private static bool Equals(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, StringComparison comparison)
        {
            // TODO: (Low) (Path) Roots should be compared separatey?
            // TODO: (Low) (Path) GetHashCode also should be calculated separately (for rootpath & relativepath)?

            var i1 = 0;
            var i2 = 0;

            while (i1 < s1.Length && i2 < s2.Length)
            {
                // look for s1 segment
                var is1 = i1;
                while (i1 < s1.Length && IsDirectorySeparator(s1[i1])) i1++;
                var hasS1 = is1 != i1;

                // look for s2 segment
                var is2 = i2;
                while (i2 < s2.Length && IsDirectorySeparator(s2[i2])) i2++;
                var hasS2 = is2 != i2;

                // Separators are matched (e.g. present on both sides, or absent on both sides)
                if (hasS1 != hasS2)
                {
                    return false;
                }

                var s1StartIndex = i1;
                while (i1 < s1.Length && !IsDirectorySeparator(s1[i1])) i1++;
                var s1Length = i1 - s1StartIndex;

                var s2StartIndex = i2;
                while (i2 < s2.Length && !IsDirectorySeparator(s2[i2])) i2++;
                var s2Length = i2 - s2StartIndex;

                // compare segments
                if (s1Length != s2Length)
                {
                    return false;
                }

                if (!s1.Slice(s1StartIndex, s1Length)
                    .Equals(s2.Slice(s2StartIndex, s2Length), comparison))
                    return false;
            }

            return i1 == s1.Length
                && i2 == s2.Length;
        }

        #endregion

        #region StartsWith

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StartsWith(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, PathComparison comparison)
            => StartsWith(s1, s2, GetStringComparison(comparison));

        public static int StartsWith(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, StringComparison comparison)
        {
            // TODO: (Low) (Path) compare roots once they been supported
            var i1 = GetRootLength(s1);
            var i2 = GetRootLength(s2);

            if (i1 != i2)
            {
                if (i1 > 1 || i2 > 1)
                {
                    // TODO: need check that both roots are same
                    throw Error.NotImplemented("Rooted path not supported yet.");
                }

                // absolute vs relative or relative vs absolte => they are considered completely unrelated.
                return -1;
            }

            while (i2 < s2.Length)
            {
                // look for s2 segment
                while (i2 < s2.Length && IsDirectorySeparator(s2[i2])) i2++;

                var s2StartIndex = i2;
                while (i2 < s2.Length && !IsDirectorySeparator(s2[i2])) i2++;
                var s2Length = i2 - s2StartIndex;
                if (s2Length == 0)
                {
                    return i1;
                }

                // look for s1 segment
                while (i1 < s1.Length && IsDirectorySeparator(s1[i1])) i1++;

                var s1StartIndex = i1;
                while (i1 < s1.Length && !IsDirectorySeparator(s1[i1])) i1++;
                var s1Length = i1 - s1StartIndex;

                if (s1Length != s2Length)
                {
                    return -1;
                }

                if (!s1.Slice(s1StartIndex, s1Length)
                    .Equals(s2.Slice(s2StartIndex, s2Length), comparison))
                    return -1;
            }

            return i1;
        }

        #endregion

        #region EndsWith

        public static bool EndsWith(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, StringComparison comparison)
        {
            // TODO: This is incorrect implementation, comparison should be done per-segment.
            // this fails on: (EndsWith("foo/bar", "/bar", any))

            if (path1.EndsWith(path2, comparison))
            {
                var p1Length = path1.Length;
                var p2Length = path2.Length;

                if (p1Length > p2Length)
                {
                    // TODO: Also check RootLength/RootNames, e.g. "c:some.txt" also EndsWith "some.txt".
                    return IsDirectorySeparator(path1[p1Length - p2Length - 1]);
                }
                else if (p1Length == p2Length)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region GetHashCode

        private const int CharacterHashShift = 5;
        private const int DirectorySeparatorHashShift = 1;

        public static int GetHashCodeOrdinal(ReadOnlySpan<char> path)
        {
            int hash = 0;
            var i = 0;

            while (i < path.Length)
            {
                while (i < path.Length && !IsDirectorySeparator(path[i]))
                {
                    hash ^= (hash << CharacterHashShift) ^ path[i];
                    i++;
                }

                // Skip separators
                var separatorStartIndex = i;
                while (i < path.Length && IsDirectorySeparator(path[i])) i++;

                // Should include each segment's separator into hash, so
                // "/foo/bar", "/foo/bar/", "foo/bar/" and "foo/bar" is all
                // different cases.
                if (i != separatorStartIndex)
                {
                    hash ^= (hash << DirectorySeparatorHashShift) ^ '/';
                }
            }

            return hash;
        }

        public static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> path)
        {
            int hash = 0;
            var i = 0;

            while (i < path.Length)
            {
                while (i < path.Length && !IsDirectorySeparator(path[i]))
                {
                    hash ^= (hash << CharacterHashShift) ^ (path[i] | 0x20); // cheap lowercase
                    i++;
                }

                // Skip separators
                var separatorStartIndex = i;
                while (i < path.Length && IsDirectorySeparator(path[i])) i++;

                if (i != separatorStartIndex)
                {
                    hash ^= (hash << DirectorySeparatorHashShift) ^ '/';
                }
            }

            return hash;
        }

        #endregion

        #region Validation

        public static bool Validate(ReadOnlySpan<char> path, PathValidations options, out PathValidations result)
        {
            result = PathValidations.None;

            var oNormalized = (options & PathValidations.Normalized) != 0;

            bool hasRootName;
            bool isAbsolute;
            bool isRelative;

            var rootLength = PathInternal.GetRootLength(path);
            if (rootLength == 0)
            {
                hasRootName = false;
                isAbsolute = false;
                isRelative = true;
            }
            else
            {
                hasRootName = rootLength > 1;
                isAbsolute = IsDirectorySeparator(path[rootLength - 1]);
                isRelative = !isAbsolute;
                if (isAbsolute && !IsValidDirectorySeparator(path[rootLength - 1], options, ref result))
                {
                    return false;
                }
            }

            {
                var expectedHasRootName = (options & PathValidations.HasRootName) != 0;
                if (expectedHasRootName && hasRootName != expectedHasRootName)
                {
                    result |= PathValidations.HasRootName;
                    return false;
                }
            }

            {
                var expectedIsAbsolute = (options & PathValidations.Absolute) != 0;
                if (expectedIsAbsolute && isAbsolute != expectedIsAbsolute)
                {
                    result |= PathValidations.Absolute;
                    return false;
                }
            }

            {
                var expectedIsRelative = (options & PathValidations.Relative) != 0;
                if (expectedIsRelative && isRelative != expectedIsRelative)
                {
                    result |= PathValidations.Relative;
                    return false;
                }
            }

            // TODO: root name also should be validated about separators, but we skip it


            if (path.Length > 0)
            {
                var leadingChar = path[0];
                if ((options & PathValidations.NoLeadingWhiteSpace) != 0
                    && char.IsWhiteSpace(leadingChar))
                {
                    result |= PathValidations.NoLeadingWhiteSpace;
                    return false;
                }
            }

            var i = 0;
            while (i < path.Length)
            {
                var separatorStartIndex = i;
                while (i < path.Length && IsDirectorySeparator(path[i]))
                {
                    if (!IsValidDirectorySeparator(path[i], options, ref result))
                    {
                        return false;
                    }
                    i++;
                }
                if ((i - separatorStartIndex) > 1 && oNormalized)
                {
                    result |= PathValidations.Normalized;
                    return false;
                }

                var segmentStartIndex = i;
                while (i < path.Length && !IsDirectorySeparator(path[i])) i++;
                var segmentLength = i - segmentStartIndex;

                var segmentIsRelative = false;

                if (segmentLength == 1)
                {
                    if (path[segmentStartIndex] == '.')
                    {
                        segmentIsRelative = true;
                    }
                }
                else if (segmentLength == 2)
                {
                    if (path[segmentStartIndex] == '.'
                        && path[segmentStartIndex + 1] == '.')
                    {
                        segmentIsRelative = true;
                    }
                }

                if (oNormalized && segmentIsRelative)
                {
                    result |= PathValidations.Normalized;
                    return false;
                }

                if (segmentLength > 0)
                {
                    var leadingChar = path[segmentStartIndex];
                    if ((options & PathValidations.SegmentNoLeadingWhiteSpace) != 0 
                        && char.IsWhiteSpace(leadingChar))
                    {
                        result |= PathValidations.SegmentNoLeadingWhiteSpace;
                        return false;
                    }
                }

                if (segmentLength > 1)
                {
                    var trailingChar = path[segmentStartIndex + segmentLength - 1];
                    if ((options & PathValidations.SegmentNoTrailingWhiteSpace) != 0
                        && char.IsWhiteSpace(trailingChar))
                    {
                        result |= PathValidations.SegmentNoTrailingWhiteSpace;
                        return false;
                    }

                    if ((options & PathValidations.SegmentNoTrailingDot) != 0
                        && !segmentIsRelative
                        && trailingChar == '.')
                    {
                        result |= PathValidations.SegmentNoTrailingDot;
                        return false;
                    }
                }

                // Analyse segment
                for (var j = segmentStartIndex; j < i; j++)
                {
                    var ch = path[j];
                    if (((options & PathValidations.AsciiChars) != 0) && ch >= 0x80)
                    {
                        result |= PathValidations.AsciiChars;
                        return false;
                    }
                    if (((options & PathValidations.FileNameCharacters) != 0)
                        && IsInvalidPathOrFileNameChar(ch))
                    {
                        result |= PathValidations.FileNameCharacters;
                        return false;
                    }
                    if (((options & PathValidations.LowerInvariantChars) != 0)
                        && char.ToLowerInvariant(ch) != ch)
                    {
                        result |= PathValidations.LowerInvariantChars;
                        return false;
                    }
                }
            }

            return result == PathValidations.None;

            static bool IsValidDirectorySeparator(char ch, PathValidations validations, ref PathValidations outputValidations)
            {
                if (ch == DirectorySeparatorChar)
                {
                    if ((validations & PathValidations.AltDirectorySeparator) != 0)
                    {
                        outputValidations |= PathValidations.AltDirectorySeparator;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (ch == AltDirectorySeparatorChar)
                {
                    if ((validations & PathValidations.DirectorySeparator) != 0)
                    {
                        outputValidations |= PathValidations.DirectorySeparator;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else return false;
            }
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


        #endregion

        #region Root

        // This code similar to dotnet/runtime/src/libraries/System.Private.CoreLib/src/System/IO/PathInternal.Windows.cs
        // However GetRootLength always includes trailing slash.

        public static int GetRootLength(ReadOnlySpan<char> path)
        {
            var pathLength = path.Length;

            bool deviceSyntax = IsDevice(path);
            bool deviceUnc = deviceSyntax && IsDeviceUNC(path);

            if ((!deviceSyntax || deviceUnc) && pathLength > 0 && IsDirectorySeparator(path[0]))
            {
                // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")
                if (deviceUnc || (pathLength > 1 && IsDirectorySeparator(path[1])))
                {
                    // UNC (\\?\UNC\ or \\), scan past server\share

                    // Start past the prefix ("\\" or "\\?\UNC\")
                    var i = deviceUnc ? UncExtendedPrefixLength : UncPrefixLength;
                    var initialLength = i;

                    // Skip two separators at most
                    int n = 2;
                    while (i < pathLength && (!IsDirectorySeparator(path[i]) || --n > 0))
                        i++;

                    // If there is another separator take it, as long as we have had at least one
                    // non-separator after the prefix (e.g. don't take "\\?\\", but take "\\?\a\")
                    if (i < pathLength && i > initialLength && IsDirectorySeparator(path[i]))
                        i++;

                    return i;
                }
                else
                {
                    // Current drive rooted (e.g. "\foo")
                    return 1;
                }
            }
            else if (deviceSyntax)
            {
                // Device path (e.g. "\\?\.", "\\.\")
                // Skip any characters following the prefix that aren't a separator
                var i = DevicePrefixLength;
                while (i < pathLength && !IsDirectorySeparator(path[i])) i++;

                // If there is another separator take it, as long as we have had at least one
                // non-separator after the prefix (e.g. don't take "\\?\\", but take "\\?\a\")
                if (i < pathLength && i > DevicePrefixLength && IsDirectorySeparator(path[i]))
                    i++;

                return i;
            }
            else if (pathLength >= 2
                && path[1] == VolumeSeparatorChar
                && IsVolume(path[0]))
            {
                if (pathLength > 2 && IsDirectorySeparator(path[2]))
                {
                    // "C:\", "D:\", etc.
                    return 3;
                }
                else return 2;
            }
            else if (pathLength > 0)
            {
                return IsDirectorySeparator(path[0]) ? 1 : 0;
            }
            else return 0;
        }

        /// <summary>
        /// Returns true if the path uses any of the DOS device path syntaxes.
        /// ("\\.\", "\\?\", or "\??\")
        /// </summary>
        private static bool IsDevice(ReadOnlySpan<char> path)
        {
            // If the path begins with any two separators is will be recognized and normalized and prepped with
            // "\??\" for internal usage correctly. "\??\" is recognized and handled, "/??/" is not.
            return IsExtended(path)
                ||
                (
                    path.Length >= DevicePrefixLength
                    && IsDirectorySeparator(path[0])
                    && IsDirectorySeparator(path[1])
                    && (path[2] == '.' || path[2] == '?')
                    && IsDirectorySeparator(path[3])
                );
        }

        /// <summary>
        /// Returns true if the path is a device UNC (\\?\UNC\, \\.\UNC\)
        /// </summary>
        private static bool IsDeviceUNC(ReadOnlySpan<char> path)
        {
            return path.Length >= UncExtendedPrefixLength
                && IsDevice(path)
                && IsDirectorySeparator(path[7])
                && path[4] == 'U'
                && path[5] == 'N'
                && path[6] == 'C';
        }

        /// <summary>
        /// Returns true if the path uses the canonical form of extended syntax ("\\?\" or "\??\").
        /// If the path matches exactly (cannot use alternate directory separators) Windows will
        /// skip normalization and path length checks.
        /// </summary>
        private static bool IsExtended(ReadOnlySpan<char> path)
        {
            // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
            // Skipping of normalization will *only* occur if back slashes ('\') are used.
            return path.Length >= DevicePrefixLength
                && path[0] == '\\'
                && (path[1] == '\\' || path[1] == '?')
                && path[2] == '?'
                && path[3] == '\\';
        }

        #endregion

        #region Character Classes

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDirectorySeparator(char ch)
        {
            return ch == DirectorySeparatorChar
                || ch == AltDirectorySeparatorChar;
        }

        private static bool IsVolume(char ch)
        {
            return ('A' <= ch && ch <= 'Z')
                || ('a' <= ch && ch <= 'z');
        }

        #endregion

        #region Helpers

        public static StringComparison GetStringComparison(PathComparison comparison)
        {
            switch (comparison)
            {
                case PathComparison.Ordinal: return StringComparison.Ordinal;
                case PathComparison.OrdinalIgnoreCase: return StringComparison.OrdinalIgnoreCase;
                default: throw Error.Argument(nameof(comparison));
            }
        }

        #endregion
    }
}
