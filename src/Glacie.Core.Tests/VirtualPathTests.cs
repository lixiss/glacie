using Microsoft.VisualBasic;

using Xunit;

namespace Glacie.Tests
{
    [Trait("Category", "Core")]
    public class VirtualPathTests
    {

        [Fact]
        public void NormalizeDefault()
        {
            VirtualPath vp = "Some\\Path/With";

            Assert.Equal(vp.Value, vp.Normalize(VirtualPathNormalization.Default));
        }


        [Fact]
        public void NormalizeDirectorySeparator()
        {
            VirtualPath vp = "Some\\Path";

            Assert.Equal("Some/Path", vp.Normalize(VirtualPathNormalization.DirectorySeparator));
        }

        [Fact]
        public void NormalizeLowerInvariant()
        {
            VirtualPath vp = "Some\\Path/With";

            Assert.Equal("some\\path/with", vp.Normalize(VirtualPathNormalization.LowerInvariant));
        }

        [Fact]
        public void NormalizeStandardForm()
        {
            VirtualPath vp = "Some\\Path/With";

            Assert.Equal("some/path/with", vp.Normalize(VirtualPathNormalization.Standard));
        }

        [Fact]
        public void EqualityOrdinal()
        {
            VirtualPath vp1 = "some/path";
            VirtualPath vp2 = "some/path";
            VirtualPath vp3 = "some\\path";

            Assert.True(vp1.Equals(vp2, VirtualPathComparison.Ordinal));
            Assert.False(vp1.Equals(vp3, VirtualPathComparison.Ordinal));
        }

        [Fact]
        public void EqualityOrdinalIgnoreCase()
        {
            VirtualPath vp1 = "some/path";
            VirtualPath vp2 = "Some/Path";
            VirtualPath vp3 = "Some\\Path";

            Assert.True(vp1.Equals(vp2, VirtualPathComparison.OrdinalIgnoreCase));
            Assert.False(vp1.Equals(vp3, VirtualPathComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void EqualityOrdinalIgnoreDirectorySeparator()
        {
            VirtualPath vp1 = "some/path";
            VirtualPath vp2 = "some\\path";
            VirtualPath vp3 = "Some\\Path";

            Assert.True(vp1.Equals(vp2, VirtualPathComparison.OrdinalIgnoreDirectorySeparator));
            Assert.False(vp1.Equals(vp3, VirtualPathComparison.OrdinalIgnoreDirectorySeparator));
        }

        [Fact]
        public void EqualityOrdinalIgnoreCaseAndDirectorySeparator()
        {
            VirtualPath vp1 = "some/path1";
            VirtualPath vp2 = "SOME\\Path1";
            VirtualPath vp3 = "Some\\Path2";

            Assert.True(vp1.Equals(vp2, VirtualPathComparison.OrdinalIgnoreCaseAndDirectorySeparator));
            Assert.False(vp1.Equals(vp3, VirtualPathComparison.OrdinalIgnoreCaseAndDirectorySeparator));
        }

        [Theory]
        [InlineData("some/path", "Some", VirtualPathComparison.NonStandard, true)]
        [InlineData("some/path", "Some\\", VirtualPathComparison.NonStandard, true)]
        [InlineData("some/path", "some1", VirtualPathComparison.Ordinal, false)]
        [InlineData("some1/path", "some", VirtualPathComparison.Ordinal, false)]
        public void StartsWithSegment(string path, string startsWithValue, VirtualPathComparison comparison, bool expected)
        {
            VirtualPath vp = path;
            Assert.Equal(expected, vp.StartsWithSegment(startsWithValue, comparison));
        }

        [Theory]
        [InlineData("some/path", "some", "path")]
        [InlineData("some1/path", "some", "some1/path")]
        [InlineData("some/path", "some/path", "")]
        [InlineData("some/path//\\", "some/path", "")]
        public void TrimStartSegment(string path, string trimWithValue, string expected)
        {
            VirtualPath vp = path;
            Assert.Equal(expected, vp.TrimStartSegment(trimWithValue, VirtualPathComparison.NonStandard));
        }

        [Theory]
        [InlineData("some/path", "Some", VirtualPathComparison.NonStandard, true)]
        [InlineData("some/path", "Some\\", VirtualPathComparison.NonStandard, true)]
        [InlineData("some/path", "some1", VirtualPathComparison.Ordinal, false)]
        [InlineData("some1/path", "some", VirtualPathComparison.Ordinal, true)]
        public void StartWith(string path, string startsWithValue, VirtualPathComparison comparison, bool expected)
        {
            VirtualPath vp = path;
            Assert.Equal(expected, vp.StartsWith(startsWithValue, comparison));
        }

        [Theory]
        [InlineData("some/path", "some", "/path")]
        [InlineData("some1/path", "some", "1/path")]
        [InlineData("some/path", "some/path", "")]
        [InlineData("some/path//\\", "some/path", "//\\")]
        public void TrimStart(string path, string trimWithValue, string expected)
        {
            VirtualPath vp = path;
            Assert.Equal(expected, vp.TrimStart(trimWithValue, VirtualPathComparison.NonStandard));
        }
    }
}
