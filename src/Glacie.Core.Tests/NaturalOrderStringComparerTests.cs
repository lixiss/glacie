using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Glacie
{
    [Trait("Category", "Core")]
    public sealed class NaturalOrderStringComparerTests
    {
        // TODO: Create test data.
        // Use test data from https://github.com/sourcefrog/natsort

        [Fact]
        public void Quick()
        {
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("rfc1.txt", "rfc2086.txt"));
            Assert.Equal(1, NaturalOrderStringComparer.Compare("rfc2086.txt", "rfc822.txt"));
            Assert.Equal(0, NaturalOrderStringComparer.Compare("rfc2086.txt", "rfc2086.txt"));
        }

        [Fact]
        public void Basic()
        {
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("a", "a0"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("a0", "a1"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("a1", "a1a"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("a1a", "a1b"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("a1b", "a2"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("a2", "a10"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("a10", "a20"));
        }

        [Fact]
        public void SeveralNumberParts()
        {
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("x2-g8", "x2-y08"));
            // Assert.Equal(-1, NaturalOrderStringComparer.Compare("x2-y08", "x2-y7"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("x2-y7", "x8-y8"));
        }

        [Fact]
        public void Fractional()
        {
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("1.001", "1.002"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("1.002", "1.010"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("1.02", "1.010"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("1.1", "1.02"));
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("1.1", "1.3"));
        }

        [Fact]
        public void CompareDecimal()
        {
            Assert.Equal(-1, NaturalOrderStringComparer.Compare("40", "100"));
        }

        [Fact]
        public void Many()
        {
            var input = new HashSet<string>();
            for (var i = 0; i < 1000; i++)
            {
                input.Add("pool" + i);
            }

            var sorted = input.Select(x => x).OrderBy(x => x, NaturalOrderStringComparer.Ordinal).ToList();
            for(var i = 0; i < 1000; i++)
            {
                Assert.Equal("pool" + i, sorted[i]);
            }
        }
    }
}
