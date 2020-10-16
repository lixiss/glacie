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
    public class Path1Benchmarks
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

        [Benchmark(Baseline = true)]
        public void ToNoneForm()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                Path1 p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.Any);
            }
        }

        #region Path

        [Benchmark]
        public void A_NRS_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.Normalized);
            }
        }

        [Benchmark]
        public void B_NRS_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.Normalized);
            }
        }

        [Benchmark]
        public void A_NRS_DS_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.DirectorySeparator);
            }
        }

        [Benchmark]
        public void B_NRS_DS_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.DirectorySeparator);
            }
        }

        [Benchmark]
        public void A_NRS_DS_LI_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.DirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void B_NRS_DS_LI_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.DirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void C_NRS_DS_LI_Path()
        {
            for (var i = 0; i < _upper.Length; i++)
            {
                var p = Path1.From(_upper[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.DirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void A_NRS_ADS_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.AltDirectorySeparator);
            }
        }

        [Benchmark]
        public void B_NRS_ADS_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.AltDirectorySeparator);
            }
        }

        [Benchmark]
        public void A_NRS_ADS_LI_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.AltDirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void B_NRS_ADS_LI_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.Normalized | Path1Form.AltDirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void A_DS_LI_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.DirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void B_DS_LI_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.DirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void A_ADS_LI_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.AltDirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void B_ADS_LI_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.AltDirectorySeparator | Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void A_DS_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.DirectorySeparator);
            }
        }

        [Benchmark]
        public void B_ADS_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.AltDirectorySeparator);
            }
        }

        [Benchmark]
        public void A_LI_Path()
        {
            for (var i = 0; i < _alreadyNormalized.Length; i++)
            {
                var p = Path1.From(_alreadyNormalized[i]);
                var x = p.ToForm(Path1Form.LowerInvariant);
            }
        }

        [Benchmark]
        public void B_LI_Path()
        {
            for (var i = 0; i < _manyRelatives.Length; i++)
            {
                var p = Path1.From(_manyRelatives[i]);
                var x = p.ToForm(Path1Form.LowerInvariant);
            }
        }

        #endregion
    }
}
