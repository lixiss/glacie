using System;
using System.Diagnostics.CodeAnalysis;

namespace Glacie
{
    public static partial class Check
    {
        // [System.Diagnostics.DebuggerStepThrough]
        [System.Diagnostics.DebuggerHidden]
        public static void True([DoesNotReturnIf(false)] bool value)
        {
            if (!value)
                throw new InvalidOperationException("Check failed.");
        }

        public static void Equal(int actual, int expected)
        {
            if (actual != expected)
                throw new InvalidOperationException("Check failed.");
        }
    }
}
