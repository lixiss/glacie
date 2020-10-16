using System;

namespace Glacie
{
    /// <summary>
    /// Each flag stands to validation subject.
    /// If during validation subject of flag is not met conditions,
    /// then validation fails and report this flag.
    /// This mean what calling validation with None - it always succeed and do
    /// nothing.
    /// </summary>
    [Flags]
    public enum PathValidations
    {
        None = 0,

        /// <summary>
        /// Rooted path, e.g. "C:" is rooted relative path,
        /// while "C:\" is rooted absolute path.
        /// </summary>
        HasRootName = 1 << 0,

        /// <summary>
        /// Absolute path.
        /// </summary>
        /// <remarks>Absolute path may have relative segments.</remarks>
        Absolute = 1 << 1,

        /// <summary>
        /// Relative path.
        /// </summary>
        Relative = 1 << 2,

        /// <summary>
        /// Normalized path never have relative segments
        /// and directory separators never appear in series
        /// (except for rooted path which may starts with two slashes).
        /// </summary>
        /// <remarks>If <see cref="DirectorySeparator"/> or <see cref="AltDirectorySeparator"/>
        /// is not specified, then both separators may appear.</remarks>
        Normalized = 1 << 3,

        /// <summary>
        /// Ensure what given path uses only <see cref="Path1.DirectorySeparatorChar"/>,
        /// and doesn't uses <see cref="Path1.AltDirectorySeparatorChar"/>.
        /// </summary>
        DirectorySeparator = 1 << 4,

        /// <summary>
        /// Ensure what given path uses only <see cref="Path1.AltDirectorySeparatorChar"/>,
        /// and doesn't uses <see cref="Path1.DirectorySeparatorChar"/>.
        /// </summary>
        /// <remarks>Flags <see cref="DirectorySeparator"/> and <see cref="AltDirectorySeparator"/>
        /// are mutually exclusive, and first takes precedence.</remarks>
        AltDirectorySeparator = 1 << 5,

        /// <summary>
        /// Path segments should contain only ASCII characters.
        /// </summary>
        AsciiChars = 1 << 6,

        /// <summary>
        /// Path segments should contain only valid filename characters.
        /// </summary>
        FileNameCharacters = 1 << 7,

        /// <summary>
        /// All segment characters should be in lower case.
        /// </summary>
        LowerInvariantChars = 1 << 8,


        SegmentNoLeadingWhiteSpace = 1 << 9,
        SegmentNoTrailingWhiteSpace = 1 << 10,
        SegmentNoTrailingDot = 1 << 11,

        /// <summary>
        /// Same as SegmentNoLeadingWhiteSpace, but applied before checks.
        /// </summary>
        NoLeadingWhiteSpace = 1 << 12,




        // There is might be more rules.
        // No leading or trailing spaces
        // No trailing dots in segments

        // TODO: See https://docs.microsoft.com/en-us/dotnet/standard/io/file-path-formats
        // Trim characters
        // Along with the runs of separators and relative segments removed earlier, some additional characters are removed during normalization:
        // If a segment ends in a single period, that period is removed. (A segment of a single or double period is normalized in the previous step. A segment of three or more periods is not normalized and is actually a valid file/directory name.)
        // If the path doesn't end in a separator, all trailing periods and spaces (U+0020) are removed. If the last segment is simply a single or double period, it falls under the relative components rule above.
        // This rule means that you can create a directory name with a trailing space by adding a trailing separator after the space.
    }
}
