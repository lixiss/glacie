using System.Collections.Generic;
using System.IO;

using Glacie.Testing;

using Xunit;

namespace Glacie.Data.Arc.Tests.Validation
{
    // This test ensures what all entries are stored in lower case,
    // and use forward slash as path separator.
    // TQAE: /Toolset/Tutorials.arc preserves file name casing.

    [Trait("Category", "ARC.Validation")]
    [TestCaseOrderer(AlphabeticalOrderer.TypeName, AlphabeticalOrderer.AssemblyName)]
    public sealed class EntryNamingTests
    {
        [Theory(DisplayName = nameof(EntryNaming)), MemberData(nameof(GetInputFileNames))]
        public void EntryNaming(TestFileInfo testFile)
        {
            // Skip this case, this archive actually preserves entry name casing.
            if (testFile.FullName == "{tqae}/Toolset/Tutorials.arc")
            {
                return;
            }

            using var archiveStream = File.Open(testFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = ArcArchive.Open(archiveStream, new ArcArchiveOptions
            {
                Mode = ArcArchiveMode.Read,
            });

            foreach (var entry in archive.GetEntries())
            {
                var entryNameInLowerCase = entry.Name.ToLowerInvariant();
                Assert.Equal(entryNameInLowerCase, entry.Name);
                Assert.False(entry.Name.Contains('\\'));
            }
        }

        private static IEnumerable<object[]> GetInputFileNames()
        {
            foreach (var testFile in TestSuiteData.GetAllArcFileNames())
            {
                yield return new object[] { testFile };
            }
        }
    }
}
