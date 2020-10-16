using System;
using System.Runtime.CompilerServices;

namespace Glacie
{
    // Path = RootName (option) / RootDirectoryChar (optional) / RelativePath
    // Path = RootPath / RelativePath

    // Kind
    // Rooted Absolute: C:\foo.bar
    // Rooted Relative: C:foo.bar
    // Absolute: /foo.bar
    // Relative: foo.bar

    partial struct Path
    {
        public static ReadOnlySpan<char> GetRootPath(string? path)
        {
            var rootLength = PathInternal.GetRootLength(path);
            if (rootLength == 0) return "";
            return path!.Substring(0, rootLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDirectorySeparator(char ch)
        {
            return ch == DirectorySeparatorChar
                || ch == AltDirectorySeparatorChar;
        }

        public static bool IsRooted(Path path)
            => IsRooted(path._value!);

        public static bool IsRooted(string path)
        {
            var rootLength = PathInternal.GetRootLength(path);
            return rootLength > 0;
        }

        public static bool IsAbsolute(Path path)
            => IsAbsolute(path._value!);

        public static bool IsAbsolute(string path)
        {
            var rootLength = PathInternal.GetRootLength(path);
            return rootLength > 0
                && IsDirectorySeparator(path[rootLength - 1]);
        }

        public static bool IsRelative(string path)
        {
            var rootLength = PathInternal.GetRootLength(path);
            return rootLength == 0
                || !IsDirectorySeparator(path[rootLength - 1]);
        }

        // ToForm(PathForm)
        // IsInForm(...)
        // ToFormNonEmpty()

        // Equals(string path1, string path2, PathComparison comparison)
        #region Equals

        public static bool Equals(Path path1, Path path2, PathComparison comparison)
            => Equals(path1.Value, path2.Value, comparison);

        public static bool Equals(string? path1, string? path2, PathComparison comparison)
        {
            switch (comparison)
            {
                case PathComparison.Ordinal:
                    return PathInternal.EqualsOrdinal(path1.AsSpan(), path2.AsSpan());

                case PathComparison.OrdinalIgnoreCase:
                    return PathInternal.EqualsOrdinalIgnoreCase(path1.AsSpan(), path2.AsSpan());

                default:
                    throw Error.Argument(nameof(comparison));
            }
        }

        public static bool Equals(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, PathComparison comparison)
        {
            switch (comparison)
            {
                case PathComparison.Ordinal:
                    return PathInternal.EqualsOrdinal(path1, path2);

                case PathComparison.OrdinalIgnoreCase:
                    return PathInternal.EqualsOrdinalIgnoreCase(path1, path2);

                default:
                    throw Error.Argument(nameof(comparison));
            }
        }

        #endregion

        #region Compare

        // Compare(...)

        #endregion

        #region GetHashCode

        // TODO: Current implementation doesn't handle root in special way, there is bit incorrectly.

        public static int GetHashCode(string path)
        {
            return GetHashCode(path.AsSpan());
        }

        public static int GetHashCode(string path, PathComparison comparison)
        {
            return GetHashCode(path.AsSpan(), comparison);
        }

        public static int GetHashCode(ReadOnlySpan<char> path)
        {
            return PathInternal.GetHashCodeOrdinal(path);
        }

        public static int GetHashCode(ReadOnlySpan<char> path, PathComparison comparison)
        {
            switch (comparison)
            {
                case PathComparison.Ordinal:
                    return PathInternal.GetHashCodeOrdinal(path);

                case PathComparison.OrdinalIgnoreCase:
                    return PathInternal.GetHashCodeOrdinalIgnoreCase(path);

                default:
                    throw Error.Argument(nameof(comparison));
            }
        }

        #endregion

        #region StartsWith

        public static bool StartsWith(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, PathComparison comparison)
        {
            return PathInternal.StartsWith(path1, path2, PathInternal.GetStringComparison(comparison)) != -1;
        }

        #endregion

        #region EndsWith

        public static bool EndsWith(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, PathComparison comparison)
        {
            return PathInternal.EndsWith(path1, path2, PathInternal.GetStringComparison(comparison));
        }
        // EndsWith

        #endregion

        #region TrimStart

        // TODO: done in path, but should have implementation here

        #endregion

        #region GetExtension

        public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
        {
            // Work over filename component
            // Scan for first dot from end, but stop on directory separator.

            var i = path.Length - 1;
            while (i > 0)
            {
                var ch = path[i];
                if (ch == '.')
                {
                    var startIndex = i;
                    var length = path.Length - startIndex;

                    i--;
                    //if (i == -1)
                    //{
                    //    break;
                    //}
                    if (i >= 0)
                    {
                        if (IsDirectorySeparator(path[i]))
                        {
                            break;
                        }
                        else if (length == 1
                            && path[i] == '.' &&
                            (i == 0 || (i > 0 && IsDirectorySeparator(path[i - 1]))))
                        {
                            break;
                        }
                    }

                    if (length == 2 && path[startIndex] == '.' && path[startIndex + 1] == '.')
                    {
                        break;
                    }

                    return path.Slice(startIndex, path.Length - startIndex);
                }
                else if (IsDirectorySeparator(ch))
                {
                    break;
                }
                i--;
            }
            return "";
        }

        #endregion

        #region Join

        public static Path Join(Path path1, Path path2)
        {
            if (path2.IsEmpty) return path1;
            if (path1.IsEmpty) return path2;

            // TODO: (High) (Path) Join
            return new Path(System.IO.Path.Combine(path1.Value, path2.Value));
        }

        public static Path Join(Path path1, string path2)
        {
            if (string.IsNullOrEmpty(path2)) return path1;
            if (path1.IsEmpty) return new Path(path2);

            // TODO: (High) (Path) Join
            return new Path(System.IO.Path.Combine(path1.Value, path2));
        }

        public static Path Join(string? path1, Path path2)
        {
            if (path2.IsEmpty) return new Path(path1);
            if (string.IsNullOrEmpty(path1)) return path2;

            // TODO: (High) (Path) Join
            return new Path(System.IO.Path.Combine(path1, path2.Value ?? ""));
        }

        public static Path Join(string? path1, string? path2)
        {
            if (string.IsNullOrEmpty(path2)) return new Path(path1);
            if (string.IsNullOrEmpty(path1)) return new Path(path2);

            // TODO: (High) (Path) Join
            return new Path(System.IO.Path.Combine(path1 ?? "", path2 ?? ""));
        }

        public static Path Join(Path path1, string? path2, char preferredDirectorySeparator)
        {
            if (string.IsNullOrEmpty(path2)) return path1;
            if (path1.IsEmpty) return new Path(path2);

            // TODO: Create join which can work more robust, but using preferred or overridable separator:
            // See https://en.cppreference.com/w/cpp/filesystem/path/operator_slash

            // TODO: (High) (Path) Join
            return new Path(System.IO.Path.Combine(path1.Value, path2));
        }

        #endregion

        #region GetRelativePath

        // GetRelativePath(string relativeTo, string path)
        public static Path GetRelativePath(in Path relativeTo, in Path path)
        {
            // TODO: (High) (Path) GetRelativePath
            // TODO: implement right algorithm https://en.cppreference.com/w/cpp/filesystem/path/lexically_normal

            // TODO: Throw exceptions, but also create non-throwing version (TryGetRelativePath).

            var pRelativeTo = relativeTo.Convert(PathConversions.Normalized);
            var pPath = path.Convert(PathConversions.Normalized);

            // TODO: Try to detect correct directory separator

            // TODO: can just compare bits
            if (IsAbsolute(pRelativeTo) != IsAbsolute(pPath)) return new Path(null);
            if (IsRooted(pRelativeTo) != IsRooted(pPath)) return new Path(null);

            return new Path(System.IO.Path.GetRelativePath(relativeTo.Value ?? "", path.Value ?? ""));
        }

        public static Path GetRelativePath(Path relativeTo, string path)
            => GetRelativePath(relativeTo, Path.Implicit(path));

        public static Path GetRelativePath(string relativeTo, Path path)
            => GetRelativePath(Path.Implicit(relativeTo), path);

        public static Path GetRelativePath(string relativeTo, string path)
            => GetRelativePath(Path.Implicit(relativeTo), Path.Implicit(path));

        #endregion

        #region EndsInDirectorySeparator

        public static bool EndsInDirectorySeparator(Path path)
        {
            return EndsInDirectorySeparator(path._value.AsSpan());
        }

        public static bool EndsInDirectorySeparator(string path)
        {
            return EndsInDirectorySeparator(path.AsSpan());
        }

        public static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
        {
            return path.Length > 0
                && IsDirectorySeparator(path[path.Length - 1]);
        }

        #endregion

        public static char GetPreferredDirectorySeparator(PathConversions form)
        {
            if ((form & PathConversions.DirectorySeparator) != 0)
                return DirectorySeparatorChar;
            else if ((form & PathConversions.AltDirectorySeparator) != 0)
                return AltDirectorySeparatorChar;
            else return DirectorySeparatorChar;
        }

        public static bool Validate(string path, PathValidations options)
            => Validate(path.AsSpan(), options, out var _);

        public static bool Validate(string path, PathValidations options, out PathValidations result)
            => Validate(path.AsSpan(), options, out result);

        public static bool Validate(ReadOnlySpan<char> path, PathValidations options)
            => Validate(path, options, out var _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Validate(ReadOnlySpan<char> path, PathValidations options, out PathValidations result)
            => PathInternal.Validate(path, options, out result);
    }
}
