using System;
using System.Runtime.CompilerServices;

namespace Glacie.Utilities
{
    public static class TimestampUtilities
    {
        // See: dotnet/runtime/src/libraries/System.Private.CoreLib/src/System/DateTime.cs
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;
        private const int MillisPerSecond = 1000;
        private const int MillisPerMinute = MillisPerSecond * 60;
        private const int MillisPerHour = MillisPerMinute * 60;
        private const int DaysPerYear = 365;
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097
        private const int DaysTo1601 = DaysPer400Years * 4;          // 584388
        private const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059
        private const long MaxTicks = DaysTo10000 * TicksPerDay - 1;
        private const long FileTimeOffset = DaysTo1601 * TicksPerDay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTimeOffset ToDateTimeOffest(long value)
        {
            return DateTimeOffset.FromFileTime(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long FromDateTimeOffset(DateTimeOffset value)
        {
            return value.ToFileTime();
        }

        public static bool TryConvert(long value, out DateTimeOffset result)
        {
            if (value < 0 || value > MaxTicks - FileTimeOffset)
            {
                result = default;
                return false;
            }

            result = ToDateTimeOffest(value);
            return true;
        }
    }
}
