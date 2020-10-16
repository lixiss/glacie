using System;

using Xunit;

namespace Glacie.Tests
{
    [Trait("Category", "Core")]
    public class Path1Tests
    {
        [Theory]
        [InlineData("/some", true)]
        [InlineData("\\some", true)]
        [InlineData("some", false)]

        //[InlineData("//myserver/file", true)] // TODO: handle correctly multiple roots

        //[InlineData("c:/some", true)] // TODO: handle windows paths, unc paths...
        //[InlineData("c:\\some", true)] // TODO: handle windows paths too
        //[InlineData("c:some", false)] // TODO: handle windows paths too
        public void Absolute(string path, bool absolute)
        {
            Assert.Equal(absolute, Path1.From(path).IsAbsolute);
            Assert.Equal(!absolute, Path1.From(path).IsRelative);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]

        [InlineData("abc/def", "abc/def")]
        [InlineData("abc///def", "abc/def")]
        [InlineData("abc\\//def", "abc\\def")]

        [InlineData("/", "/")]
        [InlineData("/.", "/")]

        [InlineData("///", "/")]

        [InlineData("abc/./def", "abc/def")]
        [InlineData("abc/.//def", "abc/def")]

        [InlineData("./abc/def", "abc/def")]
        [InlineData("abc/def/.", "abc/def/")]
        [InlineData("/./abc/def", "/abc/def")]
        [InlineData("/abc/def/.", "/abc/def/")]

        [InlineData("foo/./bar/..", "foo/")]
        [InlineData("foo/.///bar/../", "foo/")]
        [InlineData("foo/.///bar/.", "foo/bar/")]
        [InlineData("foo/.///bar/./", "foo/bar/")]

        [InlineData("///abc", "/abc")]

        public void NormalizeGeneric(string? path, string? expected)
        {
            var actual1 = Path1.From(path).ToForm(Path1Form.Normalized);
            Assert.Equal(expected, actual1.Value);
            Assert.True((actual1.Form & Path1Form.Normalized) == Path1Form.Normalized);

            var actual2 = Path1.From(path).ToForm(Path1Form.Normalized | Path1Form.Strict);
            Assert.Equal(expected, actual2.Value);
            Assert.True((actual2.Form & (Path1Form.Normalized | Path1Form.Strict))
                == (Path1Form.Normalized | Path1Form.Strict));
        }

        [Theory]
        [InlineData(".", ".")]
        public void NormalizeNonNormalized(string? path, string? expected)
        {
            var actual2 = Path1.From(path).ToForm(Path1Form.Normalized | Path1Form.Strict);
            Assert.Equal(expected, actual2.Value);
            Assert.True((actual2.Form & (Path1Form.Normalized | Path1Form.Strict)) == 0);
        }

        [Theory]
        [InlineData("/foo/../bar/../../../inv", "/inv")]
        [InlineData("/..", "/")]
        [InlineData("/../", "/")]
        public void NormalizeAbsoluteNonStrict(string? path, string? expected)
        {
            var actual = Path1.From(path).ToForm(Path1Form.Normalized);
            Assert.Equal(expected, actual.Value);
            Assert.True((actual.Form & (Path1Form.Normalized | Path1Form.Strict)) == Path1Form.Normalized);
        }

        [Theory]
        [InlineData("foo/../bar/../../../inv", "../../inv")]
        [InlineData("..", "..")]
        [InlineData("../", "..")]
        public void NormalizeRelativeNonStrict(string? path, string? expected)
        {
            var actual = Path1.From(path).ToForm(Path1Form.Normalized);
            Assert.Equal(expected, actual.Value);
            Assert.True((actual.Form & (Path1Form.Normalized | Path1Form.Strict)) == 0);
        }

        [Theory]
        [InlineData("/foo/../..", "/..")]

        [InlineData("/foo/../bar/../../../inv", "/../../inv")]
        [InlineData("foo/../bar/../../../inv", "../../inv")]
        [InlineData("..", "..")]
        [InlineData("../", "..")]
        [InlineData("/..", "/..")]
        [InlineData("/../", "/..")]
        public void NormalizeStrict(string? path, string? expected)
        {
            var actual = Path1.From(path).ToForm(Path1Form.Normalized | Path1Form.Strict);
            Assert.Equal(expected, actual.Value);

            // Path was normalized, but ends in non-normalized form.
            Assert.True((actual.Form & (Path1Form.Normalized | Path1Form.Strict)) == 0);
        }

        [Theory]
        [InlineData("abcdef", "abcdef")]
        [InlineData("ABCDEF", "abcdef")]
        [InlineData("aBcDeF", "abcdef")]
        [InlineData("AbCdEf", "abcdef")]
        public void LowerInvariant(string? path, string? expected)
        {
            Assert.Equal(expected, Path1.From(path)
                .ToForm(Path1Form.Normalized | Path1Form.LowerInvariant).Value);
        }

        [Fact]
        public void FormShouldKeepFlags()
        {
            var p1 = Path1.From("Abc\\Def");
            var p2 = p1.ToForm(Path1Form.LowerInvariant);
            var p3 = p2.ToForm(Path1Form.Normalized | Path1Form.DirectorySeparator);
            Assert.Equal(Path1Form.Relative
                | Path1Form.Normalized
                | Path1Form.LowerInvariant
                | Path1Form.DirectorySeparator, p3.Form);
        }


        [Theory]
        [InlineData(null, null, Path1Comparison.OrdinalIgnoreCase, true)]
        [InlineData("", "", Path1Comparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "Some", Path1Comparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "Some\\", Path1Comparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "some1", Path1Comparison.Ordinal, false)]
        [InlineData("some1/path", "some", Path1Comparison.Ordinal, false)]
        [InlineData("some1/\\//\\/path", "some1/path//", Path1Comparison.Ordinal, true)]

        // Absolute vs non-absolute
        [InlineData("some1/path", "/some1/path", Path1Comparison.Ordinal, false)]
        [InlineData("/some1/path", "some1/path", Path1Comparison.Ordinal, false)]

        // Both absolute
        [InlineData("/some1/path", "/some1/path", Path1Comparison.Ordinal, true)]
        [InlineData("///some1/path", "//some1/path", Path1Comparison.Ordinal, true)]
        public void StartsWithSegment(string path, string startsWithValue, Path1Comparison comparison, bool expected)
        {
            var vp = Path1.From(path);
            Assert.Equal(expected, vp.StartsWith(Path1.From(startsWithValue), comparison));
        }

        [Theory]
        [InlineData("some/path", "some", "path")]
        [InlineData("some1/path", "some", "some1/path")]
        [InlineData("some/path", "some/path", "")]
        [InlineData("some/path//\\", "some/path", "")]
        [InlineData("/some/path//\\", "//", "some/path//\\")]
        public void TrimStartSegment(string path, string trimWithValue, string expected)
        {
            var vp = Path1.From(path);
            Assert.Equal(expected, vp.TrimStart(Path1.From(trimWithValue), Path1Comparison.OrdinalIgnoreCase).Value);
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, "", true)]
        [InlineData("some/path", "some/path", true)]
        [InlineData("abc/path", "def/path", false)]
        [InlineData("/some/path", "some/path", false)]
        [InlineData("some/path/", "some/path", false)]
        [InlineData("///some\\path", "\\some/path", true)]
        public void Equality(string s1, string s2, bool expected)
        {
            var p1 = Path1.From(s1);
            var p2 = Path1.From(s2);
            Assert.Equal(expected, p1.Equals(p2));
            Assert.Equal(expected, p2.Equals(p1));
        }

        [Theory]

        // Left path preferred.
        [InlineData(null, null, null)]
        [InlineData("", null, "")]
        [InlineData(null, "", null)]
        [InlineData("", "", "")]

        [InlineData(null, "some/path", "some/path")]
        [InlineData("some/path", null, "some/path")]
        public void Join(string p1, string p2, string? expected)
        {
            Path1 actual = Path1.Join(p1, p2);
            Assert.Equal(expected, actual.ToString());

            Assert.Equal(
                (expected ?? "").Replace('\\', '/'),
                System.IO.Path.Join(p1, p2).Replace('\\', '/'));
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "", ".")]
        [InlineData("/some/base", "/some/actual/path", "../actual/path")]
        [InlineData("some/base", "some/actual/path", "../actual/path")]

        // Unrelated by Absolute
        [InlineData("/some/base", "some/actual/path", null)]
        [InlineData("some/base", "/some/actual/path", null)]
        public void GetRelativePath(string relativeTo, string path, string? expected)
        {
            Path1 actual = Path1.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, actual.ToString());
        }
    }
}
