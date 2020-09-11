using Glacie.Data.Arz.Utilities;
using Xunit;

namespace Glacie.Data.Arz.Tests
{
    [Trait("Category", "ARZ")]
    public sealed class ArzVerifierTests
    {
        [Fact]
        public void ValidEmptyFile()
        {
            ArzVerifier.Verify(TestData.GtdTqae0);
        }

        [Fact]
        public void ValidNonEmptyFile()
        {
            ArzVerifier.Verify(TestData.GtdTqae2);
        }

        [Fact]
        public void InvalidChecksum()
        {
            var ex = Assert.Throws<ArzException>(
                () => ArzVerifier.Verify(TestData.GtdTqgd0NC)
                );
            Assert.Equal("InvalidChecksum", ex.ErrorCode);
        }

        [Fact]
        public void ValidChecksum()
        {
            ArzVerifier.Verify(TestData.GtdTqgd0);
        }

        // TODO: (ArzVeriferTests) Add more tests over invalid data.
    }
}
