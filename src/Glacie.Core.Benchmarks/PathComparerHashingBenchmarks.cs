using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using IO = System.IO;

namespace Glacie.Core.Benchmarks
{
    [MemoryDiagnoser]
    public class PathComparerHashingBenchmarks
    {
        private string[] _alreadyNormalized;
        private string[] _manyRelatives;
        private string[] _upper;

        [GlobalSetup]
        public void Setup()
        {
            _alreadyNormalized = IO.File.ReadAllLines(@"G:\Glacie\glacie-test-data\core\file-names.txt");
            _manyRelatives = IO.File.ReadAllLines(@"G:\Glacie\glacie-test-data\core\file-names-with-relative-paths.txt");
            _upper = IO.File.ReadAllLines(@"G:\Glacie\glacie-test-data\core\file-names-with-relative-paths.txt")
                .Select(x => x.ToUpperInvariant()).ToArray();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        private void DoHashing(IEqualityComparer<string> equalityComparer)
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                equalityComparer.GetHashCode(_alreadyNormalized[i]);
            }
            for (var i = 0; i < _upper.Length; i++)
            {
                equalityComparer.GetHashCode(_upper[i]);
            }
        }

        private void DoHashing(IEqualityComparer<Path> equalityComparer)
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                equalityComparer.GetHashCode(new Path(_alreadyNormalized[i]));
            }
            for (var i = 0; i < _upper.Length; i++)
            {
                equalityComparer.GetHashCode(new Path(_upper[i]));
            }
        }

        [Benchmark(Baseline = true)]
        public void String_Ordinal()
        {
            DoHashing(StringComparer.Ordinal);
        }

        [Benchmark]
        public void String_OrdinalIgnoreCase()
        {
            DoHashing(StringComparer.OrdinalIgnoreCase);
        }

        [Benchmark]
        public void String_PathOrdinal()
        {
            DoHashing((IEqualityComparer<string>)PathComparer.Ordinal);
        }

        [Benchmark]
        public void String_PathOrdinalIgnoreCase()
        {
            DoHashing((IEqualityComparer<string>)PathComparer.OrdinalIgnoreCase);
        }

        [Benchmark]
        public void Path_PathOrdinal()
        {
            DoHashing((IEqualityComparer<Path>)PathComparer.Ordinal);
        }

        [Benchmark]
        public void Path_PathOrdinalIgnoreCase()
        {
            DoHashing((IEqualityComparer<Path>)PathComparer.OrdinalIgnoreCase);
        }
    }
}
