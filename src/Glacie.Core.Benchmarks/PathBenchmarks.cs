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
    public class PathBenchmarks
    {
        private string[] _alreadyNormalized;
        private string[] _manyRelatives;
        private string[] _upper;

        private Dictionary<string, bool> _stringComparerOrdinal;
        private Dictionary<string, bool> _stringComparerOrdinalIgnoreCase;
        private Dictionary<string, bool> _stringPathComparerOrdinal;
        private Dictionary<string, bool> _stringPathComparerOrdinalIgnoreCase;
        private Dictionary<Path, bool> _pathComparerOrdinal;
        private Dictionary<Path, bool> _pathComparerOrdinalIgnoreCase;

        [GlobalSetup]
        public void Setup()
        {
            _alreadyNormalized = IO.File.ReadAllLines(@"G:\Glacie\glacie-test-data\core\file-names.txt");
            _manyRelatives = IO.File.ReadAllLines(@"G:\Glacie\glacie-test-data\core\file-names-with-relative-paths.txt");
            _upper = IO.File.ReadAllLines(@"G:\Glacie\glacie-test-data\core\file-names-with-relative-paths.txt")
                .Select(x => x.ToUpperInvariant()).ToArray();

            _stringComparerOrdinal = new Dictionary<string, bool>(StringComparer.Ordinal);
            _stringComparerOrdinalIgnoreCase = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            _stringPathComparerOrdinal = new Dictionary<string, bool>(PathComparer.Ordinal);
            _stringPathComparerOrdinalIgnoreCase = new Dictionary<string, bool>(PathComparer.OrdinalIgnoreCase);
            _pathComparerOrdinal = new Dictionary<Path, bool>(PathComparer.Ordinal);
            _pathComparerOrdinalIgnoreCase = new Dictionary<Path, bool>(PathComparer.OrdinalIgnoreCase);
            FillDictionary(_stringComparerOrdinal, _alreadyNormalized);
            FillDictionary(_stringComparerOrdinalIgnoreCase, _alreadyNormalized);
            FillDictionary(_stringPathComparerOrdinal, _alreadyNormalized);
            FillDictionary(_stringPathComparerOrdinalIgnoreCase, _alreadyNormalized);
            FillDictionary(_pathComparerOrdinal, _alreadyNormalized);
            FillDictionary(_pathComparerOrdinalIgnoreCase, _alreadyNormalized);
        }

        private void FillDictionary(Dictionary<string, bool> dict, IEnumerable<string> values)
        {
            foreach (var v in values) dict.Add(v, true);
        }

        private void FillDictionary(Dictionary<Path, bool> dict, IEnumerable<string> values)
        {
            foreach (var v in values) dict.Add(new Path(v), true);
        }

        private void DoLookup(Dictionary<string, bool> dict)
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                dict.TryGetValue(_alreadyNormalized[i], out var _);
            }

            for (var i = 0; i < _upper.Length; i++)
            {
                dict.TryGetValue(_upper[i], out var _);
            }
        }
        private void DoLookup(Dictionary<Path, bool> dict)
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                dict.TryGetValue(new Path(_alreadyNormalized[i]), out var _);
            }

            for (var i = 0; i < _upper.Length; i++)
            {
                dict.TryGetValue(new Path(_upper[i]), out var _);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        [Benchmark(Baseline = true)]
        public void String_StringOrdinal()
        {
            DoLookup(_stringComparerOrdinal);
        }

        [Benchmark]
        public void String_StringOrdinalIgnoreCase()
        {
            DoLookup(_stringComparerOrdinalIgnoreCase);
        }

        [Benchmark]
        public void String_PathOrdinal()
        {
            DoLookup(_stringPathComparerOrdinal);
        }

        [Benchmark]
        public void String_PathOrdinalIgnoreCase()
        {
            DoLookup(_stringPathComparerOrdinalIgnoreCase);
        }

        [Benchmark]
        public void Path_PathOrdinal()
        {
            DoLookup(_pathComparerOrdinal);
        }

        [Benchmark]
        public void Path_PathOrdinalIgnoreCase()
        {
            DoLookup(_pathComparerOrdinalIgnoreCase);
        }
    }
}
