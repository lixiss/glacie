using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Glacie
{
    public static partial class DebugCheck
    {
        // [System.Diagnostics.DebuggerStepThrough]
        [Conditional("DEBUG"), DebuggerHidden]
        public static void True([DoesNotReturnIf(false)] bool value)
        {
            if (!value)
                throw new InvalidOperationException("Check failed.");
        }

        [Conditional("DEBUG"), DebuggerHidden]
        public static void Equal(int actual, int expected)
        {
            if (actual != expected)
                throw new InvalidOperationException("Check failed.");
        }
    }
}
