using System;

namespace Glacie.CommandLine.UI
{
    // Si:
    // kilo (k) - 10^3
    // mega (M) - 10^6
    // giga (G) - 10^9
    // tera (T) - 10^12
    // peta (P) - 10^15
    // exa (E)  - 10^18
    // zetta (Z) - 10^21
    // yotta (Y) - 10^24
    // 1 (no prefix)  10^0
    // 0.1	deci(d)    10^-1
    // 0.01	centi(c)   10^-2
    // 0.001	milli(m)   10^-3
    // 0.000 001	micro(µ)   10^-6     // us in ascii? but in utf render normal.
    // 0.000 000 001	nano(n)    10^-9
    // 0.000 000 000 001	pico(p)    10^-12
    // 0.000 000 000 000 001	femto(f)   10^-15
    // 0.000 000 000 000 000 001	atto(a)    10^-18
    // 0.000 000 000 000 000 000 001	zepto(z)   10^-21
    // 0.000 000 000 000 000 000 000 001	yocto(y)   10^-24

    internal static class UnitFormatter
    {
        private static readonly string[] _siUnits = new string[] { "", "k", "M", "G", "T", "P", "E", "Z", "Y" };
        private static readonly string[] _iecByteUnits = new string[] { "", "Ki", "Mi", "Gi", "Ti", "Pi", "Ei", "Zi", "Yi" };

        public static string FormatUnit(double value, string? suffix, string[]? units = null, double? divisor = null)
        {
            return FormatUnitCore(value, suffix, units ?? _siUnits, divisor ?? 1000.0);
        }

        private static string FormatUnitCore(double value, string? suffix, string[] units, double divisor)
        {
            var unitCount = units.Length - 1;
            for (var i = 0; i < unitCount; i++)
            {
                var absValue = Math.Abs(value);
                if (absValue < 999.5)
                {
                    if (absValue < 99.5)
                    {
                        if (absValue < 9.995)
                        {
                            // {0,-1:F2}{1}{2}"
                            return string.Format("{0:F2}{1}{2}", value, units[i], suffix);
                        }
                        // {0,-2:F1}{1}{2}
                        return string.Format("{0:F1}{1}{2}", value, units[i], suffix);
                    }
                    // "{0,-3:F0}{1}{2}"
                    return string.Format("{0:F0}{1}{2}", value, units[i], suffix);
                }
                value /= divisor;
            }

            // "{0,-3:F1}{1}{2}"
            return string.Format("{0:F1}{1}{2}", value, units[units.Length - 1], suffix);
        }

        public static string FormatSi(double value, string? suffix = null)
        {
            return FormatUnit(value, suffix, _siUnits, 1000.0);
        }

        public static string FormatIecBytes(double value, string? suffix = null)
        {
            return FormatUnit(value, suffix, _iecByteUnits, 1024.0);
        }

        // TOOD: Not used but it might be better for some cases.
        private static string FormatBytes(long value)
        {
            // TODO: invariant culture
            if (value < 1024)
            {
                return string.Format("{0:F}B", value);
            }
            else if ((value >> 10) < 1024)
            {
                return string.Format("{0:F1}KiB", value / 1024.0);
            }
            else if ((value >> 20) < 1024)
            {
                return string.Format("{0:F1}MiB", (value >> 10) / 1024.0);
            }
            else if ((value >> 30) < 1024)
            {
                return string.Format("{0:F1}GiB", (value >> 20) / 1024.0);
            }
            else if ((value >> 40) < 1024)
            {
                return string.Format("{0:F1}TiB", (value >> 30) / 1024.0);
            }
            else if ((value >> 50) < 1024)
            {
                return string.Format("{0:F1}PiB", (value >> 40) / 1024.0);
            }
            else // if ((value >> 60) < 1024)
            {
                return string.Format("{0:F1}EiB", (value >> 50) / 1024.0);
            }
        }
    }
}
