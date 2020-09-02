using System.Collections.Generic;
using System.IO;

using Glacie.Testing;

using Xunit;
using Xunit.Abstractions;

namespace Glacie.Data.Arc.Tests.Validation
{
    // This test ensures asks GetLayoutInfo over various known files.

    [Trait("Category", "ARC.Validation")]
    [TestCaseOrderer(AlphabeticalOrderer.TypeName, AlphabeticalOrderer.AssemblyName)]
    public sealed class LayoutInfoTests
    {
        private readonly ITestOutputHelper Output;

        public LayoutInfoTests(ITestOutputHelper output)
        {
            Output = output;
        }

        [Theory(DisplayName = nameof(LayoutInfo)), MemberData(nameof(GetInputFileNames))]
        public void LayoutInfo(TestFileInfo testFile)
        {
            Output.WriteLine("Archive: {0}", testFile.FullName);

            using var archiveStream = File.Open(testFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = ArcArchive.Open(archiveStream, new ArcArchiveOptions
            {
                Mode = ArcArchiveMode.Read,
            });

            var layoutInfo = archive.GetLayoutInfo();
            Output.WriteLine("         EntryCount: {0}", layoutInfo.EntryCount);
            Output.WriteLine("  RemovedEntryCount: {0}", layoutInfo.RemovedEntryCount);
            Output.WriteLine("         ChunkCount: {0}", layoutInfo.ChunkCount);
            Output.WriteLine("     LiveChunkCount: {0}", layoutInfo.LiveChunkCount);
            Output.WriteLine("UnorderedChunkCount: {0}", layoutInfo.UnorderedChunkCount);
            Output.WriteLine("  FreeFragmentCount: {0}", layoutInfo.FreeSegmentCount);
            Output.WriteLine("  FreeFragmentBytes: {0:N0}", layoutInfo.FreeSegmentBytes);

            if (testFile.FullName == "{tqae}/Resources/SharedResources.arc"
                || testFile.FullName == "{tq}/Immortal Throne/Resources/XPack/Allskins.arc"
                || testFile.FullName == "{tq}/Immortal Throne/Resources/XPack/Quests.arc"
                || testFile.FullName == "{tq}/Resources/Creatures.arc"
                || testFile.FullName == "{tq}/Resources/Effects.arc"
                || testFile.FullName == "{tq}/Resources/Items.arc")
            {
                Assert.True(layoutInfo.CanCompact);
            }
            else
            {
                Assert.False(layoutInfo.CanCompact);
            }

            Assert.False(layoutInfo.CanDefragment);
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
