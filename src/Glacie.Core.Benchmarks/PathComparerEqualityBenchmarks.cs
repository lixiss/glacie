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
    public class PathComparerEqualityBenchmarks
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

        private void DoEquals(IEqualityComparer<string> equalityComparer)
        {
            for (var i = 0; i < _alreadyNormalized.Length / 10; i++)
            {
                for (var j = 0; j < _upper.Length / 10; j++)
                {
                    equalityComparer.Equals(_alreadyNormalized[i], _upper[j]);
                }
            }
        }

        private void DoEquals(IEqualityComparer<Path> equalityComparer)
        {
            for (var i = 0; i < _alreadyNormalized.Length / 10; i++)
            {
                for (var j = 0; j < _upper.Length / 10; j++)
                {
                    equalityComparer.Equals(new Path(_alreadyNormalized[i]), new Path(_upper[j]));
                }
            }
        }

        [Benchmark(Baseline = true)]
        public void D_String_Ordinal()
        {
            DoEquals(StringComparer.Ordinal);
        }

        [Benchmark]
        public void D_String_OrdinalIgnoreCase()
        {
            DoEquals(StringComparer.OrdinalIgnoreCase);
        }

        [Benchmark]
        public void D_Path_PathGen2Ordinal()
        {
            DoEquals((IEqualityComparer<string>)PathComparer.Ordinal);
        }

        [Benchmark]
        public void D_Path_PathGen2OrdinalIgnoreCase()
        {
            DoEquals((IEqualityComparer<string>)PathComparer.OrdinalIgnoreCase);
        }
    }
}
