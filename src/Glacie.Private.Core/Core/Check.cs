using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Glacie
{
    public static partial class Check
    {
        // TODO: (Low) (Core) Migrate code to Check.That.
        [DebuggerHidden]
        public static void That([DoesNotReturnIf(false)] bool value)
        {
            if (!value) throw new InvalidOperationException("Check failed.");
        }

        [DebuggerHidden]
        public static void That([DoesNotReturnIf(false)] bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        // [System.Diagnostics.DebuggerStepThrough]
        [DebuggerHidden]
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
