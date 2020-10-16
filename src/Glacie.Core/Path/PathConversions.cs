using System;

namespace Glacie
{
    [Flags]
    public enum PathConversions
    {
        /// <summary>
        /// Path in any form, no actual conversion occur.
        /// </summary>
        None = 0,

        /// <summary>
        /// Rooted path, e.g. "C:" is rooted relative path,
        /// while "C:\" is rooted absolute path.
        /// </summary>
        /// <remarks>
        /// This option is not perform
        /// any actual conversion, it just reported back when conversion
        /// completed, and you can assert it.
        ///
        /// "C:foo.bar"  => Rooted | Relative
        /// "C:\foo.bar" => Rooted | Absolute
        /// "/foo.bar"   => Absolute (but has empty root name)
        /// "foo.bar"    => Relative (not rooted)
        /// </remarks>
        Rooted = 1 << 0,

        /// <summary>
        /// Absolute path.
        /// Absolute path may have relative segments.
        /// </summary>
        /// <remarks>
        /// See <see cref="Rooted"/> option to more details.
        /// </remarks>
        Absolute = 1 << 1,

        /// <summary>
        /// Relative path.
        /// </summary>
        /// <remarks>
        /// See <see cref="Rooted"/> option to more details.
        /// </remarks>
        Relative = 1 << 2,

        /// <summary>
        /// In strict form, there is no relative segments allowed, and them
        /// never folded beyond root. This means what attempt to remove
        /// a relative segment may lead to returned path keep some relative
        /// segments. (And result will not conatin nor Strict, nor Normalized
        /// flags.)
        /// </summary>
        Strict = 1 << 3,

        /// <summary>
        /// Indicates what path is in normal form.
        /// (Removes relative segments.)
        /// </summary>
        Normalized = 1 << 4,

        /// <summary>
        /// Normalize directory separator (all <see cref="Path1.AltDirectorySeparatorChar"/>
        /// will be replaced by <see cref="Path1.DirectorySeparatorChar"/>.
        /// </summary>
        DirectorySeparator = 1 << 5,

        /// <summary>
        /// Normalize directory separator (all <see cref="Path1.DirectorySeparatorChar"/>
        /// will be replaced by <see cref="Path1.AltDirectorySeparatorChar"/>.
        /// </summary>
        AltDirectorySeparator = 1 << 6,

        /// <summary>
        /// Convert all characters to lower case (using invariant culture).
        /// </summary>
        LowerInvariant = 1 << 7,
    }
}
