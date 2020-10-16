using System.Linq;

using Xunit;

using IO = System.IO;

namespace Glacie.Tests
{
    [Trait("Category", "Core")]
    public class PathTests
    {
        [Theory]
        [InlineData(@"\foo", @"\")]

        [InlineData(@"C:\Documents\Newsletters\Summer2018.pdf", @"C:\")]
        [InlineData(@"\Program Files\Custom Utilities\StringFinder.exe", @"\")]
        [InlineData(@"2018\January.xlsx", @"")]
        [InlineData(@"..\Publications\TravelBrochure.pdf", @"")]
        [InlineData(@"C:\Projects\apilibrary\apilibrary.sln", @"C:\")]
        [InlineData(@"C:Projects\apilibrary\apilibrary.sln", @"C:")]

        [InlineData(@"\\.\C:\Test\Foo.txt", @"\\.\C:\")]
        [InlineData(@"\\?\C:\Test\Foo.txt", @"\\?\C:\")]
        [InlineData(@"\??\C:\Test\Foo.txt", @"\??\C:\")]
        [InlineData(@"\\.\Volume{b75e2c83-0000-0000-0000-602f00000000}\Test\Foo.txt", @"\\.\Volume{b75e2c83-0000-0000-0000-602f00000000}\")]
        [InlineData(@"\\?\Volume{b75e2c83-0000-0000-0000-602f00000000}\Test\Foo.txt", @"\\?\Volume{b75e2c83-0000-0000-0000-602f00000000}\")]

        [InlineData(@"\\system07\C$\something", @"\\system07\C$\")]
        [InlineData(@"\\Server2\Share\Test\Foo.txt", @"\\Server2\Share\")]

        [InlineData(@"\\.\UNC\Server\Share\Test\Foo.txt", @"\\.\UNC\Server\Share\")]

        // For device UNCs, the server/share portion forms the volume.
        // For example, in \\?\server1\e:\utilities\\filecomparer\, the server/share portion is server1\utilities.
        // This is significant when calling a method such as Path.GetFullPath(String, String) with relative directory segments;
        // it is never possible to navigate past the volume.
        // E.g. not sure how it is should be properly handled.
        [InlineData(@"\\?\server1\e:\utilities\\filecomparer\", @"\\?\server1\")]
        public void GetRootPath(string path, string rootPath)
        {
            Assert.Equal(rootPath, Path.GetRootPath(path).ToString());
        }

        [Theory]
        [InlineData(@"C:\Documents\Newsletters\Summer2018.pdf", true)]
        [InlineData(@"\Program Files\Custom Utilities\StringFinder.exe", true)]
        [InlineData(@"2018\January.xlsx", false)]
        [InlineData(@"..\Publications\TravelBrochure.pdf", false)]
        [InlineData(@"C:\Projects\apilibrary\apilibrary.sln", true)]
        [InlineData(@"C:Projects\apilibrary\apilibrary.sln", true)]
        public void Rooted(string path, bool rooted)
        {
            Assert.Equal(rooted, Path.IsRooted(path));

            Assert.Equal(rooted, IO.Path.IsPathRooted(path));
        }


        [Theory]
        [InlineData("/some", true)]
        [InlineData("\\some", true)]
        [InlineData("some", false)]
        public void Absolute(string path, bool absolute)
        {
            Assert.Equal(absolute, Path.IsAbsolute(path));
            Assert.Equal(!absolute, Path.IsRelative(path));
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, "", true)]
        [InlineData("foo/bar", "foo/bar", true)]
        [InlineData("/foo/bar", "/foo/bar", true)]
        [InlineData("/foo/bar/", "/foo/bar/", true)]
        [InlineData("foo/bar/", "foo/bar/", true)]
        [InlineData("///some\\path", "\\some/path", true)]

        [InlineData("/foo/bar", "foo/bar", false)]
        [InlineData("foo/bar/", "foo/bar", false)]

        [InlineData("abc/path", "def/path", false)]
        [InlineData("/some/path", "some/path", false)]
        [InlineData("some/path/", "some/path", false)]
        public void Equality(string s1, string s2, bool expected)
        {
            Assert.Equal(expected, Path.Equals(s1, s2, PathComparison.Ordinal));
            Assert.Equal(expected, Path.Equals(s2, s1, PathComparison.Ordinal));
        }

        [Fact]
        public void GetHashCodeOrdinal()
        {
            Assert.Equal(
                Path.GetHashCode("abc/def", PathComparison.Ordinal),
                Path.GetHashCode("abc//def", PathComparison.Ordinal));

            Assert.Equal(
                Path.GetHashCode("//fileshare//someroot/under", PathComparison.Ordinal),
                Path.GetHashCode("\\fileshare/////someroot/////under", PathComparison.Ordinal));

            var cases = new int[4];
            cases[0] = Path.GetHashCode("/foo/bar", PathComparison.Ordinal);
            cases[1] = Path.GetHashCode("/foo/bar/", PathComparison.Ordinal);
            cases[2] = Path.GetHashCode("foo/bar/", PathComparison.Ordinal);
            cases[2] = Path.GetHashCode("foo/bar", PathComparison.Ordinal);
            Assert.Equal(4, cases.Distinct().Count());
        }

        [Fact]
        public void GetHashCodeOrdinalIgnoreCase()
        {
            Assert.Equal(
                Path.GetHashCode("Abc/dEf", PathComparison.OrdinalIgnoreCase),
                Path.GetHashCode("abC\\\\deF", PathComparison.OrdinalIgnoreCase));

            Assert.Equal(
                Path.GetHashCode("//fiLeshare//someRoot/uNder", PathComparison.OrdinalIgnoreCase),
                Path.GetHashCode("\\filesHare/////somerOot/////undEr", PathComparison.OrdinalIgnoreCase));

            Assert.NotEqual(
                Path.GetHashCode("/Abc/Def", PathComparison.OrdinalIgnoreCase),
                Path.GetHashCode("Abc/Def", PathComparison.OrdinalIgnoreCase));

            Assert.NotEqual(
                Path.GetHashCode("Abc/Def/", PathComparison.OrdinalIgnoreCase),
                Path.GetHashCode("Abc/Def", PathComparison.OrdinalIgnoreCase));
        }

        #region Conversions

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]

        [InlineData("abc/def", "abc/def")]
        [InlineData("abc///def", "abc/def")]
        [InlineData("abc\\//def", "abc\\def")]

        [InlineData("/", "/")]
        [InlineData("/.", "/")]

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
        public void NormalizeGeneric(string? path, string? expected)
        {
            var actual1Success = Path.Implicit(path).TryConvert(PathConversions.Normalized, out var actual1);
            Assert.Equal(expected, actual1.Value);
            Assert.True(actual1Success);

            var actual2Success = Path.Implicit(path).TryConvert(PathConversions.Normalized | PathConversions.Strict, out var actual2);
            Assert.Equal(expected, actual2.Value);
            Assert.True(actual2Success);
        }

        [Theory]
        [InlineData("///", "/")]
        [InlineData("///////.///////c:////1", "///./c:/1")]
        [InlineData("///abc", "/abc")]
        public void NormalizeRooted(string path, string expected)
        {
            var actual1Success = Path.Implicit(path).TryConvert(PathConversions.Rooted | PathConversions.Absolute | PathConversions.Normalized, out var actual1);
            Assert.Equal(expected, actual1.Value);
            Assert.True(actual1Success);

            var actual2Success = Path.Implicit(path).TryConvert(PathConversions.Normalized | PathConversions.Strict, out var actual2);
            Assert.Equal(expected, actual2.Value);
            Assert.True(actual2Success);
        }

        [Theory]
        [InlineData(".", ".")]
        public void NormalizeNonNormalized(string? path, string? expected)
        {
            var actual1Success = Path.Implicit(path).TryConvert(PathConversions.Normalized | PathConversions.Strict, out var actual1);
            Assert.False(actual1Success);
            Assert.Equal(default!, actual1);
            Assert.Equal(expected, Path.Implicit(path).Convert(PathConversions.Normalized | PathConversions.Strict).Value);
        }

        [Theory]
        [InlineData("/foo/../bar/../../../inv", "/inv")]
        [InlineData("/..", "/")]
        [InlineData("/../", "/")]
        public void NormalizeAbsoluteNonStrict(string? path, string? expected)
        {
            var actualSuccess = Path.Implicit(path).TryConvert(PathConversions.Normalized, out var actual);
            Assert.Equal(expected, actual.Value);
            Assert.True(actualSuccess);
        }

        [Theory]
        [InlineData("foo/../bar/../../../inv", "../../inv")]
        [InlineData("..", "..")]
        [InlineData("../", "..")]
        public void NormalizeRelativeNonStrict(string? path, string? expected)
        {
            var actualSuccess = Path.Implicit(path).TryConvert(PathConversions.Normalized, out var actual);
            Assert.False(actualSuccess);
            Assert.Equal(default!, actual.Value);
            Assert.Equal(expected, Path.Implicit(path).Convert(PathConversions.Normalized).Value);
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
            var actualSuccess = Path.Implicit(path)
                .TryConvert(PathConversions.Normalized | PathConversions.Strict, out var actual);
            // Path was normalized, but ends in non-normalized form.
            Assert.False(actualSuccess);
            Assert.Equal(default!, actual);
            Assert.Equal(expected, Path.Implicit(path).Convert(PathConversions.Normalized | PathConversions.Strict).Value);
        }

        #endregion

        [Theory]
        [InlineData("abcdef", "abcdef")]
        [InlineData("ABCDEF", "abcdef")]
        [InlineData("aBcDeF", "abcdef")]
        [InlineData("AbCdEf", "abcdef")]
        public void LowerInvariant(string? path, string? expected)
        {
            Assert.Equal(expected, Path.Implicit(path)
                .Convert(PathConversions.Normalized | PathConversions.LowerInvariant).Value);
        }

        [Theory]
        [InlineData(null, null, PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("", "", PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "Some", PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "Some\\", PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "some1", PathComparison.Ordinal, false)]
        [InlineData("some1/path", "some", PathComparison.Ordinal, false)]
        [InlineData("some1/\\//\\/path", "some1/path//", PathComparison.Ordinal, true)]

        // Absolute vs non-absolute
        [InlineData("some1/path", "/some1/path", PathComparison.Ordinal, false)]
        [InlineData("/some1/path", "some1/path", PathComparison.Ordinal, false)]

        // Both absolute
        [InlineData("/some1/path", "/some1/path", PathComparison.Ordinal, true)]
        [InlineData("///some1/path", "//some1/path", PathComparison.Ordinal, true)]
        public void StartsWithSegment(string path, string startsWithValue, PathComparison comparison, bool expected)
        {
            var vp = Path.Implicit(path);
            Assert.Equal(expected, vp.StartsWith(Path.Implicit(startsWithValue), comparison));
        }

        [Theory]
        [InlineData(null, null, PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("", "", PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "path", PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "1path", PathComparison.Ordinal, false)]
        [InlineData("some/1path", "path", PathComparison.Ordinal, false)]

        // Absolute vs non-absolute
        [InlineData("some1/path", "/some1/path", PathComparison.Ordinal, false)]
        [InlineData("/some1/path", "some1/path", PathComparison.Ordinal, true)]

        // Both absolute
        [InlineData("/some1/path", "/some1/path", PathComparison.Ordinal, true)]
        [InlineData("///some1/path", "//some1/path", PathComparison.Ordinal, true)]

        // Currently not implemented
        [InlineData("some/path", "/path", PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("some///path", "some/path", PathComparison.OrdinalIgnoreCase, true)]
        [InlineData("some/path", "some///path", PathComparison.OrdinalIgnoreCase, true)]
        public void EndsWithSegment(string path, string endsWithValue, PathComparison comparison, bool expected)
        {
            Assert.Equal(expected, Path.Implicit(path).EndsWith(endsWithValue, comparison));
        }

        [Theory]
        [InlineData("some/path", "some", "path")]
        [InlineData("some1/path", "some", "some1/path")]
        [InlineData("some/path", "some/path", "")]
        [InlineData("some/path//\\", "some/path", "")]
        [InlineData("/some/path//\\", "//", "some/path//\\")]
        public void TrimStartSegment(string path, string trimWithValue, string expected)
        {
            var vp = Path.Implicit(path);
            Assert.Equal(expected, vp.TrimStart(Path.Implicit(trimWithValue), PathComparison.OrdinalIgnoreCase).Value);
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
            var actual = Path.Join(p1, p2);
            Assert.Equal(expected, actual.Value);

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
            var actual = Path.GetRelativePath(relativeTo, path);
            Assert.Equal(expected, actual.ToString());
        }

        [Theory]
        [InlineData("some_path", PathValidations.Relative, PathValidations.None)]
        [InlineData("/some_path", PathValidations.Absolute, PathValidations.None)]
        [InlineData("c:some_path", PathValidations.HasRootName | PathValidations.Relative, PathValidations.None)]
        [InlineData("c:/some_path", PathValidations.HasRootName | PathValidations.Absolute, PathValidations.None)]

        [InlineData("some_path", PathValidations.HasRootName, PathValidations.HasRootName)]
        [InlineData("some_path", PathValidations.Absolute, PathValidations.Absolute)]
        [InlineData("/some_path", PathValidations.Relative, PathValidations.Relative)]

        [InlineData("foo/bar", PathValidations.Normalized, PathValidations.None)]
        [InlineData("foo//bar", PathValidations.Normalized, PathValidations.Normalized)]
        [InlineData("./foo", PathValidations.Normalized, PathValidations.Normalized)]
        [InlineData("/./foo", PathValidations.Normalized, PathValidations.Normalized)]
        [InlineData("foo/../bar", PathValidations.Normalized, PathValidations.Normalized)]
        [InlineData("foo/bar/.", PathValidations.Normalized, PathValidations.Normalized)]
        [InlineData("foo////bar", PathValidations.Normalized, PathValidations.Normalized)]

        [InlineData("foo/bar", PathValidations.DirectorySeparator, PathValidations.None)]
        [InlineData("foo/bar", PathValidations.AltDirectorySeparator, PathValidations.AltDirectorySeparator)]
        [InlineData("foo\\bar", PathValidations.DirectorySeparator, PathValidations.DirectorySeparator)]

        [InlineData("foo/абв", PathValidations.AsciiChars, PathValidations.AsciiChars)]
        [InlineData("foo/**/*.txt", PathValidations.FileNameCharacters, PathValidations.FileNameCharacters)]
        
        [InlineData("foo/bar", PathValidations.LowerInvariantChars, PathValidations.None)]
        [InlineData("foO/bAr", PathValidations.LowerInvariantChars, PathValidations.LowerInvariantChars)]

        [InlineData(" foo/bAr", PathValidations.NoLeadingWhiteSpace, PathValidations.NoLeadingWhiteSpace)]
        [InlineData("foo / bAr", PathValidations.SegmentNoTrailingWhiteSpace, PathValidations.SegmentNoTrailingWhiteSpace)]
        [InlineData("foo / bAr", PathValidations.SegmentNoLeadingWhiteSpace, PathValidations.SegmentNoLeadingWhiteSpace)]
        [InlineData("foo./bAr", PathValidations.SegmentNoTrailingDot, PathValidations.SegmentNoTrailingDot)]

        public void Validation(string path, PathValidations validations, PathValidations expectedResult)
        {
            var success = Path.Implicit(path).Validate(validations, out var actualResult);
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedResult == PathValidations.None, success);
        }


        [Theory]
        [InlineData("", "")]
        [InlineData(".", "")]
        [InlineData("..", "")]
        [InlineData("/.", "")]
        [InlineData("/..", "")]
        [InlineData(".bar", "")]
        [InlineData("/.bar", "")]
        [InlineData("/foo.bar", ".bar")]
        [InlineData("/foo/", "")]
        [InlineData("/foo/bar.txt", ".txt")]
        [InlineData("/foo/bar.", ".")]
        [InlineData("/foo/bar", "")]
        [InlineData("/foo/bar.txt/bar.cc", ".cc")]
        [InlineData("/foo/bar.txt/bar.", ".")]
        [InlineData("/foo/bar.txt/bar", "")]
        [InlineData("/foo/.", "")]
        [InlineData("/foo/..", "")]
        [InlineData("/foo/.hidden", "")]
        [InlineData("/foo/..bar", ".bar")]
        public void GetExtension(string path, string expected)
        {
            Assert.Equal(expected, Path.GetExtension(path).ToString());
        }
    }
}
