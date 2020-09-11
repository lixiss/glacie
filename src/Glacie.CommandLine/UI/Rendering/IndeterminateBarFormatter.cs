using System;

namespace Glacie.CommandLine.UI
{
    internal static class IndeterminateBarFormatter
    {
        public static string Format(int width, int frameNo)
        {
            if (width <= 0) return string.Empty;

            var w = width / 4;
            var x = (frameNo) % (width + 2 * w) - w;

            var cx1 = Math.Clamp(x, 0, width);
            var cx2 = Math.Clamp(x + w, 0, width);

            var w1 = cx1;
            var w2 = cx2 - cx1;
            var w3 = width - w1 - w2;

            DebugCheck.That(w1 + w2 + w3 == width);

            return new string(' ', w1)
                + new string('>', w2)
                + new string(' ', w3);
        }
    }
}
