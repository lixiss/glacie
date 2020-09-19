using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Glacie.Diagnostics
{
    internal static class Hash
    {
        public static int Combine(int value, int currentValue)
        {
            return unchecked((value * (int)0xA5555529) + currentValue);
        }

        public static int Combine(bool value, int current)
        {
            return Combine(current, value ? 1 : 0);
        }

        public static int Combine<T>(T? value, int current) where T : class
        {
            int hash = unchecked(current * (int)0xA5555529);

            if (value != null)
            {
                return unchecked(hash + value.GetHashCode());
            }

            return hash;
        }

        public static int CombineValues<T>(IEnumerable<T>? values, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            var result = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                if (value != null)
                {
                    result = Combine(value.GetHashCode(), result);
                }
            }

            return result;
        }

        public static int CombineValues<T>(T[]? values, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            maxItemsToHash = Math.Min(maxItemsToHash, values.Length);
            var result = 0;
            for (int i = 0; i < maxItemsToHash; i++)
            {
                T value = values[i];

                if (value != null)
                {
                    result = Combine(value.GetHashCode(), result);
                }
            }

            return result;
        }

        public static int CombineValues<T>(ImmutableArray<T> values, int maxItemsToHash = int.MaxValue)
        {
            if (values.IsDefaultOrEmpty)
            {
                return 0;
            }

            var result = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                if (value != null)
                {
                    result = Combine(value.GetHashCode(), result);
                }
            }

            return result;
        }

        public static int CombineValues(IEnumerable<string> values, StringComparer stringComparer, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            var result = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                if (value != null)
                {
                    result = Combine(stringComparer.GetHashCode(value), result);
                }
            }

            return result;
        }
    }
}
