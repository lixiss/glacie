using System;
using System.Diagnostics;

namespace Glacie
{
    // Do not annotate debug checks with DoesNotReturnIf or similar code
    // analysis attributes, as they doesn't affect release builds.

    public static partial class DebugCheck
    {
        // TODO: (Low) (Core) Add DebugCheck.That(bool).
        [Conditional("DEBUG"), DebuggerHidden]
        public static void That(bool value)
        {
            if (!value) throw new InvalidOperationException("Check failed.");
        }

        [Conditional("DEBUG"), DebuggerHidden]
        public static void That(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        // [System.Diagnostics.DebuggerStepThrough]
        [Conditional("DEBUG"), DebuggerHidden]
        public static void True(bool value)
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
