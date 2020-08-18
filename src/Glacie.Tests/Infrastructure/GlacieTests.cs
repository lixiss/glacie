using System;
using System.Linq;
using Xunit;

namespace Glacie.Tests
{
    public abstract class GlacieTests
    {
        protected static void AssertGxError(string expectedErrorCode, Action testCode)
        {
            var ex = Assert.Throws<GlacieException>(testCode);
            Assert.Equal(expectedErrorCode, ex.ErrorCode);
        }

        protected static void AssertInvariants(Database database)
        {
            var count = database.Count;
            var countByIteration = database.GetAll().Count();
            Assert.Equal(count, countByIteration);
        }
    }
}
