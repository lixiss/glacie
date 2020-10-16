using System;
using System.Collections.Generic;

namespace Glacie
{
    // |                         Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
    // |------------------------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
    // |               D_String_Ordinal |  8.295 ms | 0.0641 ms | 0.0535 ms |  1.00 |    0.00 |     - |     - |     - |         - |
    // |     D_String_OrdinalIgnoreCase |  9.818 ms | 0.0227 ms | 0.0177 ms |  1.18 |    0.01 |     - |     - |     - |     128 B |
    // |           D_Path_StringOrdinal |  8.380 ms | 0.0375 ms | 0.0313 ms |  1.01 |    0.01 |     - |     - |     - |         - |
    // | D_Path_StringOrdinalIgnoreCase | 10.135 ms | 0.0341 ms | 0.0303 ms |  1.22 |    0.01 |     - |     - |     - |      35 B |
    // |             D_Path_PathOrdinal | 22.652 ms | 0.0855 ms | 0.0758 ms |  2.73 |    0.02 |     - |     - |     - |         - |
    // |   D_Path_PathOrdinalIgnoreCase | 25.145 ms | 0.0509 ms | 0.0452 ms |  3.03 |    0.02 |     - |     - |     - |         - |

    // Try to beat this numbers!
    // Implement PathGen2 comparers

    // Hashing:
    //|                           Method |      Mean |     Error |    StdDev | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
    //|--------------------------------- |----------:|----------:|----------:|------:|------:|------:|------:|----------:|
    //|                 D_String_Ordinal |  4.797 ms | 0.0054 ms | 0.0045 ms |  1.00 |     - |     - |     - |         - |
    //|       D_String_OrdinalIgnoreCase |  6.061 ms | 0.0062 ms | 0.0051 ms |  1.26 |     - |     - |     - |       4 B |
    //|             D_Path_StringOrdinal |  4.864 ms | 0.0081 ms | 0.0076 ms |  1.01 |     - |     - |     - |         - |
    //|   D_Path_StringOrdinalIgnoreCase |  6.136 ms | 0.0062 ms | 0.0055 ms |  1.28 |     - |     - |     - |       6 B |
    //|               D_Path_PathOrdinal | 10.896 ms | 0.0107 ms | 0.0095 ms |  2.27 |     - |     - |     - |         - |
    //|     D_Path_PathOrdinalIgnoreCase | 11.475 ms | 0.0168 ms | 0.0141 ms |  2.39 |     - |     - |     - |      92 B |
    //|           D_Path_PathGen2Ordinal | 10.901 ms | 0.0183 ms | 0.0171 ms |  2.27 |     - |     - |     - |         - |
    //| D_Path_PathGen2OrdinalIgnoreCase | 11.471 ms | 0.0122 ms | 0.0108 ms |  2.39 |     - |     - |     - |         - |
    // Hashing can be improved to not scan for segments, but scan string once...

    public abstract class PathComparer
        : IEqualityComparer<string>
        , IEqualityComparer<Path>
    {
        public static PathComparer Ordinal => PathComparerOrdinal.Instance;
        public static PathComparer OrdinalIgnoreCase => PathComparerOrdinalIgnoreCase.Instance;

        public abstract bool Equals(string? x, string? y);

        public abstract int GetHashCode(string obj);

        public abstract bool Equals(Path x, Path y);

        public abstract int GetHashCode(Path obj);

        private sealed class PathComparerOrdinal : PathComparer
        {
            public static readonly PathComparerOrdinal Instance = new PathComparerOrdinal();

            private PathComparerOrdinal() { }

            public override bool Equals(string? x, string? y)
            {
                return PathInternal.EqualsOrdinal(x, y);
            }

            public override int GetHashCode(string obj)
            {
                return PathInternal.GetHashCodeOrdinal(obj);
            }

            public override bool Equals(Path x, Path y)
            {
                return PathInternal.EqualsOrdinal(x.Value, y.Value);
            }

            public override int GetHashCode(Path obj)
            {
                return PathInternal.GetHashCodeOrdinal(obj.ToString().AsSpan());
            }
        }

        private sealed class PathComparerOrdinalIgnoreCase : PathComparer
        {
            public static readonly PathComparerOrdinalIgnoreCase Instance = new PathComparerOrdinalIgnoreCase();
            private PathComparerOrdinalIgnoreCase() { }

            public override bool Equals(string? x, string? y)
            {
                return PathInternal.EqualsOrdinalIgnoreCase(x, y);
            }

            public override int GetHashCode(string obj)
            {
                return PathInternal.GetHashCodeOrdinalIgnoreCase(obj);
            }

            public override bool Equals(Path x, Path y)
            {
                return PathInternal.EqualsOrdinalIgnoreCase(x.Value, y.Value);
            }

            public override int GetHashCode(Path obj)
            {
                return PathInternal.GetHashCodeOrdinalIgnoreCase(obj.ToString().AsSpan());
            }
        }
    }
}
