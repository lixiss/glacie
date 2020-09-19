using System;
using System.Collections.Generic;

namespace Glacie
{
    // TODO: (High) (NaturalOrderStringComparer) There is naive port of original code.
    // It dirty and can be much better. Also should have support for ReadOnlySpan<char>.
    // See also: https://github.com/dotnet/runtime/issues/13979
    // Also known as alpha-numeric sorting or logical sorting.
    //
    // Basically it should find areas of digits and non-digits and compare non-digits
    // with system's string comparer. However for ordinal or ordinal ignore case
    // it should be exist more optimized versions (like implementation below).
    //
    // Current implementation was based on https://github.com/sourcefrog/natsort
    // but original code is buggy and this implementation was improved a bit
    // to have common sense, rather than attempt to compare floating point values.

    // https://en.wikipedia.org/wiki/Natural_sort_order

    public sealed class NaturalOrderStringComparer : IComparer<string?>
    {
        public static readonly NaturalOrderStringComparer Ordinal = new NaturalOrderStringComparer(false);

        public static readonly NaturalOrderStringComparer OrdinalIgnoreCase = new NaturalOrderStringComparer(true);

        public static readonly Comparison<string?> OrdinalComparison = (a, b) => Compare(a, b, false);

        public static readonly Comparison<string?> OrdinalIgnoreCaseComparison = (a, b) => Compare(a, b, true);

        public static int Compare(string? a, string? b) => Compare(a, b, false);

        public static int Compare(string? a, string? b, bool ignoreCase)
        {
            if ((object?)a == b) return 0;
            if (a == null) return -1;
            if (b == null) return +1;

            // only count the number of zeroes leading the last number compared
            var ai = 0; // SkipOverLeadingSpacesOrZeros(a, 0);
            var bi = 0; // SkipOverLeadingSpacesOrZeros(b, 0);

            while (true)
            {
                var ca = CharAt(a, ai);
                var cb = CharAt(b, bi);

                if (char.IsDigit(ca) && char.IsDigit(cb))
                {
                    var result = CompareNumerical(a, b, ignoreCase, ref ai, ref bi);
                    if (result != 0)
                        return result;

                    ca = CharAt(a, ai);
                    cb = CharAt(b, bi);
                }

                if (ignoreCase)
                {
                    ca = char.ToUpperInvariant(ca);
                    cb = char.ToUpperInvariant(cb);
                }

                if (ca < cb)
                    return -1;

                if (ca > cb)
                    return +1;

                // TODO: there is wrong in length-prefixed strings, because they may have zero chars.
                if (ca == char.MinValue && cb == char.MinValue)
                {
                    // In order if strings differs only in leading zeroes or whitespaces.
                    return a.Length - b.Length;
                }

                ++ai; ++bi;
            }
        }

        private static int CompareNumerical(string a, string b, bool ignoreCase, ref int ai, ref int bi)
        {
            var bias = 0;

            var pa = ai;
            var pb = bi;

            for (; ; pa++, pb++)
            {
                var ca = CharAt(a, pa);
                var cb = CharAt(b, pb);

                if (!char.IsDigit(ca))
                {
                    if (char.IsDigit(cb))
                        bias = -1;
                    break;
                }

                if (!char.IsDigit(cb))
                {
                    bias = 1;
                    break;
                }

                if (bias != 0)
                    continue;

                if (ignoreCase)
                {
                    ca = char.ToUpperInvariant(ca);
                    cb = char.ToUpperInvariant(cb);
                }

                if (ca < cb)
                {
                    bias = -1;
                }
                else if (ca > cb)
                {
                    bias = 1;
                }
            }

            ai = pa;
            bi = pb;
            return bias;
        }

        private static char CharAt(string text, int index)
            => (uint)index < (uint)text.Length ? text[index] : char.MinValue;

        private readonly bool _ignoreCase;

        private NaturalOrderStringComparer(bool ignoreCase)
        {
            _ignoreCase = ignoreCase;
        }

        int IComparer<string>.Compare(string x, string y) => Compare(x, y, _ignoreCase);
    }
}
