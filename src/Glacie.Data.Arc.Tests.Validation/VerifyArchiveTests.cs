using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

using Glacie.ChecksumAlgorithms;
using Glacie.Testing;

using Xunit;

namespace Glacie.Data.Arc.Tests.Validation
{
    [Trait("Category", "ARC.Validation")]
    [TestCaseOrderer(AlphabeticalOrderer.TypeName, AlphabeticalOrderer.AssemblyName)]
    public sealed class VerifyArchiveTests
    {
        private const int BufferSize = 16 * 1024;

        [Theory(DisplayName = nameof(VerifyArchive)), MemberData(nameof(GetInputFileNames))]
        public void VerifyArchive(TestFileInfo testFile)
        {
            using var archiveStream = File.Open(testFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = ArcArchive.Open(archiveStream, new ArcArchiveOptions
            {
                Mode = ArcArchiveMode.Read,
            });

            foreach (var entry in archive.GetEntries())
            {
                var result = VerifyEntry(entry);
                Assert.True(result);
            }
        }

        private bool VerifyEntry(ArcEntry entry)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            try
            {
                using var inputStream = entry.Open();
                var hash = new Adler32();
                while (true)
                {
                    var bytesInBuffer = inputStream.Read(buffer);
                    if (bytesInBuffer == 0) break;
                    hash.ComputeHash(buffer, 0, bytesInBuffer);
                }
                return entry.Hash == hash.Hash;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
