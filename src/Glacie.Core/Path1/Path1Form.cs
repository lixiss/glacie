using System;

namespace Glacie
{
    /// <summary>
    /// Defines various path forms and their conversions.
    /// </summary>
    [Flags]
    [Obsolete]
    public enum Path1Form
    {
        /// <summary>
        /// Path in any form.
        /// </summary>
        Any = 0,

        /// <summary>
        /// Rooted path, e.g. "C:" is rooted relative path,
        /// while "C:\" is rooted absolute path.
        /// </summary>
        Rooted = 1 << 0,

        /// <summary>
        /// Absolute path.
        /// Absolute path may have relative segments.
        /// </summary>
        Absolute = 1 << 1,

        /// <summary>
        /// Relative path.
        /// </summary>
        Relative = 1 << 2,

        /// <summary>
        /// In strict form, there is no relative segments allowed, and them
        /// never folded beyond root. This means what attempt to remove
        /// a relative segment may lead to returned path keep some relative
        /// segments.
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
