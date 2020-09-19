using System;

namespace Glacie
{
    // Note: Values should be in sync with VirtualPath.Flags.

    [Flags]
    public enum VirtualPathNormalization
    {
        Default = 0,

        /// <summary>
        /// Normalize directory separator (all <see cref="VirtualPath.AltDirectorySeparatorChar"/>
        /// will be replaced by <see cref="VirtualPath.DirectorySeparatorChar"/>.
        /// </summary>
        DirectorySeparator = 1 << 0,

        /// <summary>
        /// Convert all characters to lower case (invariant culture).
        /// </summary>
        LowerInvariant = 1 << 1,


        Standard = DirectorySeparator | LowerInvariant,
    }
}
