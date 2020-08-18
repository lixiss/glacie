using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Glacie
{
    public static partial class Check
    {
        public static class Argument
        {
            public static void NotNull<T>([NotNull]T value, string parameterName) where T : class?
            {
                if (value is null) throw new ArgumentNullException(parameterName);
            }

            public static void NotNullNorEmpty([NotNull] string? value, string parameterName)
            {
                if (string.IsNullOrEmpty(value)) throw ArgumentNullOrEmpty(value, parameterName);
            }

            public static void NotNullNorWhiteSpace([NotNull] string? value, string parameterName)
            {
                if (string.IsNullOrWhiteSpace(value)) throw ArgumentNullOrWhiteSpace(value, parameterName);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static ArgumentException ArgumentNullOrEmpty(string? value, string parameterName)
            {
                if (value is null)
                    return new ArgumentNullException(parameterName);
                else
                    return new ArgumentException("Value cannot be empty.", parameterName);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static ArgumentException ArgumentNullOrWhiteSpace(string? value, string parameterName)
            {
                if (value is null)
                    return new ArgumentNullException(parameterName);
                else
                    return new ArgumentException("Value cannot be whitespace.", parameterName);
            }
        }
    }
}
