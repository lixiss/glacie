using System;

namespace Glacie
{
    [Flags]
    public enum VirtualPathComparison
    {
        Ordinal,
        OrdinalIgnoreCase,
        OrdinalIgnoreDirectorySeparator,
        OrdinalIgnoreCaseAndDirectorySeparator,

        NonStandard = OrdinalIgnoreCaseAndDirectorySeparator,
    }
}
