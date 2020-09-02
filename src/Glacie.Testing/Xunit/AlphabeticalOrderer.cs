using System.Collections.Generic;
using System.Linq;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Glacie.Testing
{
    public sealed class AlphabeticalOrderer : ITestCaseOrderer
    {
        public const string AssemblyName = "Glacie.Testing";
        public const string TypeName = AssemblyName + "." + nameof(AlphabeticalOrderer);

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
            IEnumerable<TTestCase> testCases) where TTestCase : ITestCase =>
            testCases.OrderBy(testCase => testCase.DisplayName);
    }
}
