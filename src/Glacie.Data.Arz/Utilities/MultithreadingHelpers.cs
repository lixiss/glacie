using System;

namespace Glacie.Data.Arz
{
    // TODO: (Low) (Arz) (MultithreadingHelpers) another helper class with one method... Also check how it behave. TPL prefer throw on invalid values.
    internal static class MultithreadingHelpers
    {
        public static int GetEffectiveDegreeOfParallelism(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < -1) throw Error.InvalidOperation("Invalid maxDegreeOfParallelism.");

            if (maxDegreeOfParallelism == 0) return 0;

            var numberOfLogicalProcessors = Environment.ProcessorCount;
            if (maxDegreeOfParallelism == -1) return numberOfLogicalProcessors;
            if (numberOfLogicalProcessors == 1) return 0;
            return maxDegreeOfParallelism;
        }
    }
}
