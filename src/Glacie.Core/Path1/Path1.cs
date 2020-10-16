using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Glacie.Text;

namespace Glacie
{
    // TODO: Path preserves string nulls, but this doesn't always nice.
    // TODO: Also i'm like implicit string conversions, but this might not be
    // so useful. Check perfomance of Dictionary<Path, bool> when wrapping
    // string in Path struct.
    // TODO: Also ToForm & IsInForm is not a very nice combo from maintenance
    // perspective.

    [Obsolete]
    [DebuggerDisplay("{ToString()}, Form = {Form}")]
    public readonly struct Path1 : IEquatable<Path1>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Path1 From(string? value) => new Path1(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Path1 From(string value, Path1Form form) => new Path1(value).ToForm(form);

        public const char DirectorySeparatorChar = '/';
        public const char AltDirectorySeparatorChar = '\\';

        private readonly string? _path;
        private readonly Path1Form _form;

        public Path1(string? path)
        {
            _path = path;
            _form = Path1Form.Any;
        }

        private Path1(string? path, Path1Form form)
        {
            _path = path;
            _form = form;
        }

        public string? Value => _path;

        public Path1Form Form => _form;

        public bool IsRooted => GetIsRooted(_path);

        public bool IsAbsolute => GetIsAbsolute(_path);

        public bool IsRelative => !IsAbsolute;

        public bool IsEmpty => string.IsNullOrEmpty(_path);

        // ? RootName => (e.g. "C:")
        // ? RootDirectory => (e.g. "\") single char, might be absent
        // ? RootPath (root_name() / root_directory())
        // ? RelativePath (returns everything after RootPath

        public Path1 ToForm(Path1Form form)
        {
            if (form == _form || form == Path1Form.Any)
            {
                return this;
            }

            var resultPath = _path;
            Path1Form resultForm;

            if ((form & Path1Form.Normalized) != 0)
            {
                // Determine which transform needed.
                var transformMask = Path1Form.DirectorySeparator
                    | Path1Form.AltDirectorySeparator
                    | Path1Form.LowerInvariant;

                var currentState = (_form & transformMask);

                // Clear transforms which was already done.
                var normalizeByForm = form & ~currentState;

                resultPath = Normalize(resultPath, normalizeByForm, out resultForm);

                DebugCheck.That((resultForm & currentState) == 0);

                resultForm |= currentState;
            }
            else
            {
                resultForm = _form;

                // TODO: (Low) (Path) Make combined version for lower invariant + directory separator

                if ((form & Path1Form.DirectorySeparator) != 0)
                {
                    resultPath = resultPath?.Replace('\\', '/');

                    resultForm |= (form & ~Path1Form.AltDirectorySeparator)
                        | Path1Form.DirectorySeparator;
                }
                else if ((form & Path1Form.AltDirectorySeparator) != 0)
                {
                    resultPath = resultPath?.Replace('/', '\\');

                    resultForm |= (form & ~Path1Form.DirectorySeparator)
                        | Path1Form.AltDirectorySeparator;
                }

                if ((form & Path1Form.LowerInvariant) != 0)
                {
                    resultPath = resultPath?.ToLowerInvariant();

                    resultForm |= (form & ~Path1Form.LowerInvariant)
                        | Path1Form.LowerInvariant;
                }
            }

            // Adjust path type bits
            {
                resultForm &= ~(Path1Form.Rooted
                    | Path1Form.Absolute
                    | Path1Form.Relative);

                resultForm |= GetIsAbsolute(resultPath)
                    ? Path1Form.Absolute
                    : Path1Form.Relative;

                resultForm |= GetIsRooted(resultPath)
                    ? Path1Form.Rooted
                    : 0;
            }

            return new Path1(resultPath, resultForm);
        }

        public bool IsInForm(Path1Form form)
        {
            return (_form & form) == form;
        }

        /// <summary>
        /// Convert non-empty path to given form.
        /// </summary>
        public Path1 ToFormNonEmpty(Path1Form form)
        {
            if (string.IsNullOrEmpty(_path)) return this;
            return ToForm(form);
        }

        #region Equality

        public bool Equals(in Path1 other)
        {
            return Equals(in other, Path1Comparison.Ordinal);
        }

        public bool Equals(in Path1 other, Path1Comparison comparison)
        {
            return Equals(_path, other._path, GetStringComparison(comparison));
        }

        public bool Equals(string? other)
        {
            return Equals(other, Path1Comparison.Ordinal);
        }

        public bool Equals(string? other, Path1Comparison comparison)
        {
            return Equals(_path, other, GetStringComparison(comparison));
        }

        public static bool operator ==(in Path1 p1, in Path1 p2)
        {
            return p1.Equals(in p2);
        }

        public static bool operator !=(in Path1 p1, in Path1 p2)
        {
            return !p1.Equals(in p2);
        }

        /// <summary>
        /// Returns -1 when s1 doesnot starts from s2.
        /// On success returns s1 index.
        /// </summary>
        private static bool Equals(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, StringComparison comparison)
        {
            // TODO: (Low) (Path) compare roots once they been supported
            var i1 = GetRootLength(s1);
            var i2 = GetRootLength(s2);

            if (i1 != i2)
            {
                return false;
            }

            while (i1 < s1.Length)
            {
                // look for s1 segment
                var is1 = i1;
                while (i1 < s1.Length && IsDirectorySeparator(s1[i1])) i1++;

                var s1StartIndex = i1;
                while (i1 < s1.Length && !IsDirectorySeparator(s1[i1])) i1++;
                var s1Length = i1 - s1StartIndex;

                // look for s2 segment
                var is2 = i2;
                while (i2 < s2.Length && IsDirectorySeparator(s2[i2])) i2++;

                var s2StartIndex = i2;
                while (i2 < s2.Length && !IsDirectorySeparator(s2[i2])) i2++;
                var s2Length = i2 - s2StartIndex;

                // Ensure what both segments consumed
                var hasS1 = is1 != i1;
                var hasS2 = is2 != i2;
                if (!(hasS1 && hasS2))
                {
                    return false;
                }

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

        // public IEnumerable<ReadOnlyMemory<char>> Segments() => throw Error.NotImplemented();

        // public bool HasRelativeSegments() => throw Error.NotImplemented();

        public bool StartsWith(in Path1 path, Path1Comparison comparison)
        {
            return StartsWith(AsSpan(), path.AsSpan(), GetStringComparison(comparison)) != -1;
        }

        /// <summary>
        /// Returns -1 when s1 doesnot starts from s2.
        /// On success returns s1 index.
        /// </summary>
        private static int StartsWith(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, StringComparison comparison)
        {
            // TODO: (Low) (Path) compare roots once they been supported
            var i1 = GetRootLength(s1);
            var i2 = GetRootLength(s2);

            if (i1 != i2)
            {
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

        private ReadOnlySpan<char> AsSpan()
        {
            return _path.AsSpan();
        }

        public Path1 TrimStart(in Path1 path, Path1Comparison comparison)
        {
            var index = StartsWith(AsSpan(), path.AsSpan(), GetStringComparison(comparison));
            if (index >= 0)
            {
                var s = AsSpan().Slice(index);
                var i = 0;
                while (i < s.Length && IsDirectorySeparator(s[i])) i++;
                var resultPath = s.Slice(i).ToString();

                var resultForm = _form;
                // Adjust path type bits
                {
                    resultForm &= ~(Path1Form.Rooted
                        | Path1Form.Absolute
                        | Path1Form.Relative);

                    resultForm |= GetIsAbsolute(resultPath)
                        ? Path1Form.Absolute
                        : Path1Form.Relative;

                    resultForm |= GetIsRooted(resultPath)
                        ? Path1Form.Rooted
                        : 0;
                }

                return new Path1(resultPath, resultForm);
            }
            return this;
        }

        public static Path1 Join(in Path1 p1, in Path1 p2)
        {
            if (p2.IsEmpty) return p1;
            if (p1.IsEmpty) return p2;

            // TODO: (High) (Path) Join
            return new Path1(System.IO.Path.Combine(p1.Value, p2.Value));
        }

        public static Path1 Join(in Path1 p1, string? p2)
        {
            if (string.IsNullOrEmpty(p2)) return p1;
            if (p1.IsEmpty) return new Path1(p2);

            // TODO: (High) (Path) Join
            return new Path1(System.IO.Path.Combine(p1.Value, p2));
        }

        public static Path1 Join(string? p1, in Path1 p2)
        {
            if (p2.IsEmpty) return new Path1(p1);
            if (string.IsNullOrEmpty(p1)) return p2;

            // TODO: (High) (Path) Join
            return new Path1(System.IO.Path.Combine(p1, p2.Value ?? ""));
        }

        public static Path1 Join(string? p1, string? p2)
        {
            if (string.IsNullOrEmpty(p2)) return new Path1(p1);
            if (string.IsNullOrEmpty(p1)) return new Path1(p2);

            // TODO: (High) (Path) Join
            return new Path1(System.IO.Path.Combine(p1 ?? "", p2 ?? ""));
        }

        public static Path1 GetRelativePath(in Path1 relativeTo, in Path1 path)
        {
            // TODO: (High) (Path) GetRelativePath
            // TODO: implement right algorithm https://en.cppreference.com/w/cpp/filesystem/path/lexically_normal

            // TODO: Throw exceptions, but also create non-throwing version (TryGetRelativePath).

            var pRelativeTo = relativeTo.ToForm(Path1Form.Normalized);
            var pPath = path.ToForm(Path1Form.Normalized);

            // TODO: Try to detect correct directory separator

            // TODO: can just compare bits
            if (pRelativeTo.IsAbsolute != pPath.IsAbsolute) return new Path1(null);
            if (pRelativeTo.IsRooted != pPath.IsRooted) return new Path1(null);

            return new Path1(System.IO.Path.GetRelativePath(relativeTo.Value ?? "", path.Value ?? ""));
        }

        public static Path1 GetRelativePath(in Path1 relativeTo, string path)
            => GetRelativePath(in relativeTo, new Path1(path));

        public static Path1 GetRelativePath(string relativeTo, in Path1 path)
            => GetRelativePath(new Path1(relativeTo), in path);

        public static Path1 GetRelativePath(string relativeTo, string path)
            => GetRelativePath(new Path1(relativeTo), new Path1(path));

        public bool EndsInDirectorySeparator()
        {
            return EndsInDirectorySeparator(_path);
        }

        public bool EndsWith(string? value, Path1Comparison comparison)
        {
            if (value == null) return true;
            if (_path == null) return false;

            var pLength = _path.Length;
            if (pLength < value.Length) return false;

            var sComparison = GetStringComparison(comparison);
            if (_path.EndsWith(value, sComparison))
            {
                var vLength = value.Length;

                if (pLength == vLength) return true;

                if (pLength > vLength)
                {
                    // TODO: Also check RootLength/RootNames, e.g. "c:some.txt" also EndsWith "some.txt".
                    return IsDirectorySeparator(_path[pLength - vLength - 1]);
                }
            }

            return false;
        }

        public static bool EndsInDirectorySeparator(in Path1 path)
        {
            return EndsInDirectorySeparator(path._path);
        }

        public static bool EndsInDirectorySeparator(string? path)
        {
            if (path == null) return false;
            var length = path.Length;
            return length > 0 && IsDirectorySeparator(path[length - 1]);
        }

        public static char GetPreferredDirectorySeparator(Path1Form form)
        {
            if ((form & Path1Form.DirectorySeparator) != 0)
                return DirectorySeparatorChar;
            else if ((form & Path1Form.AltDirectorySeparator) != 0)
                return AltDirectorySeparatorChar;
            else return DirectorySeparatorChar;
        }

        private static bool GetIsAbsolute(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var rootLength = GetRootLength(path);
            return rootLength > 0
                && IsDirectorySeparator(path[rootLength - 1]);
        }

        private static bool GetIsRooted(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.Length >= 2)
            {
                var c0 = path[0];
                var c1 = path[1];
                if (IsDirectorySeparator(c0) && IsDirectorySeparator(c1))
                    return true;
                if (c1 == ':')
                {
                    var driveLetter = char.ToUpperInvariant(c0);
                    return ('A' <= driveLetter && driveLetter <= 'Z');
                }
            }
            return false;

            // TODO: (Low) (Path) Implement correct GetRootLength.
            //var rootLength = GetRootLength(path);
            //return rootLength > 0
            //    && IsDirectorySeparator(path[rootLength - 1]);
        }

        private static int GetRootLength(string? path)
        {
            return path != null
                && path.Length > 0
                && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        private static int GetRootLength(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        private static bool IsDirectorySeparator(char c)
        {
            return c == DirectorySeparatorChar
                || c == AltDirectorySeparatorChar;
        }


        private static string? Normalize(string? path, Path1Form form, out Path1Form resultForm)
        {
            // Normal form of an empty path is an empty path.
            if (string.IsNullOrEmpty(path))
            {
                resultForm = form;
                return path;
            }

            var sb = new ValueStringBuilder(stackalloc char[260]);

            if (Normalize(path.AsSpan(), form, ref sb, out resultForm))
            {
                path = sb.ToString();
            }

            sb.Dispose();
            return path;
        }

        private static bool Normalize(ReadOnlySpan<char> path, Path1Form form, ref ValueStringBuilder sb, out Path1Form resultForm)
        {
            var rootLength = GetRootLength(path);
            return Normalize(path, rootLength, form, ref sb, out resultForm);
        }

        private static bool PrefixEndsWithDirectorySeparator(ReadOnlySpan<char> path, int prefixLength)
        {
            return prefixLength > 0 && IsDirectorySeparator(path[prefixLength - 1]);
        }

        private static bool Normalize(ReadOnlySpan<char> path,
            int prefixLength,
            Path1Form form,
            ref ValueStringBuilder sb,
            out Path1Form resultForm)
        {
            DebugCheck.That((form & Path1Form.Normalized) != 0);

            var strict = (form & Path1Form.Strict) != 0;

            var modified = false;
            var hasNoRelativeSegments = true;

            var rootPrefixLength = prefixLength;

            var index = prefixLength;
            if (prefixLength > 0)
            {
                sb.Append(path.Slice(0, prefixLength));

                // Currently rooted path can be ends only with directory separator.
                // On windows there is other rule (c:some_path) is relative to current directory path.
                DebugCheck.That(IsDirectorySeparator(path[prefixLength - 1]));

                // After root we can have multiple directory separator chars. Skip them.
                // E.g. "//" => "/" or "/\\\\" -> "/" or "\\///" -> "\\".
                while (index < path.Length && IsDirectorySeparator(path[index])) { index++; }
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
                                        if ((form & Path1Form.DirectorySeparator) != 0)
                                        {
                                            modified |= c != DirectorySeparatorChar;
                                            c = DirectorySeparatorChar;
                                        }
                                        else if ((form & Path1Form.AltDirectorySeparator) != 0)
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
                if ((form & Path1Form.LowerInvariant) != 0)
                {
                    var segmentSpan = path.Slice(segmentStartIndex, segmentLength);
                    var i = 0;
                    while (i < segmentSpan.Length)
                    {
                        if ((uint)(segmentSpan[i] - 'A') <= (uint)('Z' - 'A'))
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

                    if ((form & Path1Form.DirectorySeparator) != 0)
                    {
                        modified |= c != DirectorySeparatorChar;
                        c = DirectorySeparatorChar;
                    }
                    else if ((form & Path1Form.AltDirectorySeparator) != 0)
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
                    ? Path1Form.Normalized | Path1Form.Strict
                    : Path1Form.Normalized;
            }
            else
            {
                resultForm = 0;
            }

            if ((form & Path1Form.DirectorySeparator) != 0)
            {
                resultForm |= Path1Form.DirectorySeparator;
            }
            else if ((form & Path1Form.AltDirectorySeparator) != 0)
            {
                resultForm |= Path1Form.AltDirectorySeparator;
            }

            if ((form & Path1Form.LowerInvariant) != 0)
            {
                resultForm |= Path1Form.LowerInvariant;
            }


            if (!modified && sb.Length == path.Length)
            {
                return false;
            }

            return true;
        }


        //A path can be normalized by following this algorithm:

        //If the path is empty, stop (normal form of an empty path is an empty path)
        //Replace each directory-separator (which may consist of multiple slashes) with a single path::preferred_separator.
        //Replace each slash character in the root-name with path::preferred_separator.
        //Remove each dot and any immediately following directory-separator.
        //Remove each non-dot-dot filename immediately followed by a directory-separator and a dot-dot, along with any immediately following directory-separator.
        //If there is root-directory, remove all dot-dots and any directory-separators immediately following them.
        //If the last filename is dot-dot, remove any trailing directory-separator.
        //If the path is empty, add a dot (normal form of ./ is .)

        #region Object

        public override int GetHashCode()
        {
            return GetHashCode(_path.AsSpan(), Path1Comparison.Ordinal);

            // TODO: (High) (Path) HashCode should be based on Equals,
            // otherwise it will not work properly in hash tables.

            return _path != null ? _path.GetHashCode() : 0;
        }

        public int GetHashCode(Path1Comparison comparison)
        {
            return GetHashCode(_path.AsSpan(), comparison);
        }

        private static int GetHashCode(ReadOnlySpan<char> path, Path1Comparison comparison)
        {
            // TODO: (Low) (Path) compare roots once they been supported
            int hash;
            var i1 = GetRootLength(path);

            hash = i1;

            while (i1 < path.Length)
            {
                // look for s1 segment
                var is1 = i1;
                while (i1 < path.Length && IsDirectorySeparator(path[i1])) i1++;

                var s1StartIndex = i1;
                while (i1 < path.Length && !IsDirectorySeparator(path[i1])) i1++;
                var s1Length = i1 - s1StartIndex;

                hash += 1 + s1Length;

                var s1 = path.Slice(s1StartIndex, s1Length);
                if (comparison == Path1Comparison.Ordinal)
                {
                    // get hash code
                    for (var i = 0; i < s1Length; i++)
                    {
                        hash ^= (hash << 5) ^ s1[i];
                    }
                }
                else if (comparison == Path1Comparison.OrdinalIgnoreCase)
                {
                    // get hash code
                    for (var i = 0; i < s1Length; i++)
                    {
                        //hash ^= (hash << 5) ^ char.ToLowerInvariant(s1[i]);
                        hash ^= (hash << 5) ^ (s1[i] ^ ~0x40);
                    }
                }
                else throw Error.InvalidOperation();
            }

            return hash;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => _path ?? "";

        public override bool Equals(object obj)
        {
            if (obj is Path1 other) return Equals(in other);
            return false;
        }

        #endregion

        #region IEquatable<Path>

        public bool Equals(Path1 other)
        {
            return Equals(in other);
        }

        #endregion

        private static StringComparison GetStringComparison(Path1Comparison comparison)
        {
            return comparison switch
            {
                Path1Comparison.Ordinal => StringComparison.Ordinal,
                Path1Comparison.OrdinalIgnoreCase => StringComparison.OrdinalIgnoreCase,
                _ => throw Error.Argument(nameof(comparison)),
            };
        }

        private static StringComparer GetStringComparer(Path1Comparison comparison)
        {
            return comparison switch
            {
                Path1Comparison.Ordinal => StringComparer.Ordinal,
                Path1Comparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
                _ => throw Error.Argument(nameof(comparison)),
            };
        }
    }
}
