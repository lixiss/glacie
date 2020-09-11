using System;

namespace Glacie.CommandLine.UI
{
    // 0x2588 - Full Block (safe to use on windows?)
    // 0x2589 - Left 7/8 block
    // 0x258A - Left 3/4 block
    // 0x258B - Left 5/8 block
    // 0x258C - Left Half Block (safe to use on windows?)
    // 0x258D - Left 3/8 block
    // 0x258E - Left 1/4 block
    // 0x258F - Left 1/8 block

    internal static class BarFormatter
    {
        public const string Ascii = " #";
        public const string AsciiFull = " 123456789#";
        public const string Utf = " \u258F\u258E\u258D\u258C\u258B\u258A\u2589\u2588";
        public const string UtfWindows = " \u258C\u2588";

        public static string Format(double value, int width, string barCharacters)
        {
            if (barCharacters == null || barCharacters.Length < 2) throw Error.Argument(nameof(barCharacters));
            if (width <= 0) return string.Empty;

            if (value < 0.0) value = 0.0;
            else if (value > 1.0)
            {
                value = 1.0;
            }

            var nBarChars = barCharacters.Length;

            // var scaledValue = width * value;
            // var fractionValue = scaledValue - Math.Truncate(scaledValue);

            // var filledCount = Math.DivRem((int)(scaledValue * nBarChars), nBarChars, out var barCharacterIndex);
            var filledCount = Math.DivRem((int)(value * width * nBarChars),
                nBarChars,
                out var barCharacterIndex);

            if (filledCount < width)
            {
                var innerChar = barCharacters[barCharacterIndex];
                var remainCount = width - filledCount - 1;

                return new string(barCharacters[barCharacters.Length - 1], filledCount)
                    + innerChar
                    + new string(barCharacters[0], remainCount);
            }
            else
            {
                return new string(barCharacters[barCharacters.Length - 1], filledCount);
            }
        }
    }
}
