using System.Globalization;
using System.Runtime.CompilerServices;

namespace Glacie
{
    // TODO: (VeryLow) Redesign Glacie.StringExtensions (to be just a helper?). Error, ArzError class still use it.
    public static class StringExtensions
    {
        private const MethodImplOptions NoInlining = MethodImplOptions.NoInlining;
        private const MethodImplOptions AggressiveInlining = MethodImplOptions.AggressiveInlining;

        [MethodImpl(AggressiveInlining)]
        public static string FormatWith(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        [MethodImpl(AggressiveInlining)]
        public static string FormatWith(this string format, object arg0)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0);
        }

        [MethodImpl(AggressiveInlining)]
        public static string FormatWith(this string format, object arg0, object arg1)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1);
        }

        [MethodImpl(AggressiveInlining)]
        public static string FormatWith(this string format, object arg0, object arg1, object arg2)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
        }

        /*
        [MethodImpl(NoInlining)]
        public static string FormatWith<T0>(this string format, T0 arg0)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1>(this string format, T0 arg0, T1 arg1)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1, T2>(this string format, T0 arg0, T1 arg1, T2 arg2)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1, T2, T3>(this string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2, arg3);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1, T2, T3, T4>(this string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2, arg3, arg4);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1, T2, T3, T4, T5>(this string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1, T2, T3, T4, T5, T6>(this string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1, T2, T3, T4, T5, T6, T7>(this string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        [MethodImpl(NoInlining)]
        public static string FormatWith<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        */
    }
}
