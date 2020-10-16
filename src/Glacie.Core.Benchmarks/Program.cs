using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Glacie.Core.Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var result = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);



            // var summary = BenchmarkRunner.Run<PathBenchmarks>();
            // PathBenchmarks.A_DS_Path2
        }
    }
}
