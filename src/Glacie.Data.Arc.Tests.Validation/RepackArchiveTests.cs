using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;

using Glacie.Data.Compression;
using Glacie.Testing;

using Xunit;
using Xunit.Abstractions;

namespace Glacie.Data.Arc.Tests.Validation
{
    [Trait("Category", "ARC.Validation")]
    [TestCaseOrderer(AlphabeticalOrderer.TypeName, AlphabeticalOrderer.AssemblyName)]
    public sealed class RepackArchiveTests
    {
        private const int BufferSize = 16 * 1024;

        private readonly ITestOutputHelper Output;

        public RepackArchiveTests(ITestOutputHelper output)
        {
            Output = output;
        }

        [Theory(DisplayName = nameof(RepackArchive)), MemberData(nameof(GetInputFileNames))]
        public void RepackArchive(TestFileInfo testFile)
        {
            using var archiveStream = ReadFile(testFile.Path);
            using var expectedArchiveStream = new MemoryStream((int)archiveStream.Length);
            archiveStream.CopyTo(expectedArchiveStream);

            var inputLength = archiveStream.Length;

            {
                using var archive = ArcArchive.Open(archiveStream, new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Update,
                });

                CompressionLevel compressionLevel;
                if (archive.GetFormat().Version == 1)
                {
                    compressionLevel = (CompressionLevel)4;
                }
                else
                {
                    compressionLevel = CompressionLevel.Fastest;
                }

                archive.Repack(compressionLevel);
                CheckCompressedLength(archive);
            }

            var outputLength = archiveStream.Length;
            Output.WriteLine("   Input Path: {0}", testFile);
            Output.WriteLine(" Input Length: {0}", inputLength);
            Output.WriteLine("Output Length: {0}", outputLength);
            Output.WriteLine("   Difference: {0} bytes", outputLength - inputLength);

            // Testing results
            {
                using var expectedArchive = ArcArchive.Open(expectedArchiveStream);
                using var actualArchive = ArcArchive.Open(archiveStream);
                CompareArchives(expectedArchive, actualArchive);

                CheckCompressedLength(actualArchive);
            }
        }

        private static MemoryStream ReadFile(string path)
        {
            using var inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);

            // Want to free previously occupied memory by memory stream.
            CollectGarbageIf(256 * 1024 * 1024 - inputStream.Length);

            var memoryStream = new MemoryStream(checked((int)inputStream.Length));
            inputStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private static void CheckCompressedLength(ArcArchive archive)
        {
            foreach (var e in archive.GetEntries())
            {
                if (e.EntryType == 3)
                {
                    long chunkCompressedLength = 0;

                    var chunkInfo = e.GetChunkInfo();
                    for (var i = 0; i < chunkInfo.Count; i++)
                    {
                        ref readonly var chunk = ref chunkInfo[i];
                        chunkCompressedLength += chunk.CompressedLength;
                    }

                    if (e.CompressedLength != chunkCompressedLength)
                        throw Error.InvalidOperation("Entry compressed length != chunk compressed length.");
                }
            }
        }

        private static void CompareArchives(ArcArchive expectedArchive, ArcArchive actualArchive)
        {
            if (expectedArchive.Count != actualArchive.Count)
                throw Error.InvalidOperation("Different number of entries.");

            foreach (var expectedEntry in expectedArchive.GetEntries())
            {
                var actualEntry = actualArchive.GetEntry(expectedEntry.Name);

                using var expectedStream = expectedEntry.Open();
                using var actualStream = actualEntry.Open();
                CompareStreamContent(expectedStream, actualStream);
            }
        }

        private static void CompareStreamContent(Stream expectedStream, Stream actualStream, int bufferSize = 16384)
        {
            var entryBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var inputBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            while (true)
            {
                var actualStreamBytesRead = actualStream.Read(entryBuffer);
                var expectedStreamBytesRead = expectedStream.Read(inputBuffer);

                if (actualStreamBytesRead == expectedStreamBytesRead)
                {
                    if (actualStreamBytesRead == 0) break;

                    for (var i = 0; i < actualStreamBytesRead; i++)
                    {
                        if (entryBuffer[i] != inputBuffer[i])
                            throw Error.InvalidOperation("Stream content differs.");
                    }
                }
                else
                {
                    // There is possible if stream returns less bytes than requested.
                    throw Error.Unreachable();
                }
            }
            ArrayPool<byte>.Shared.Return(inputBuffer);
            ArrayPool<byte>.Shared.Return(entryBuffer);
        }


        private static void CollectGarbageIf(long totalMemoryThreshold, bool waitForPendingFinalizers = false)
        {
            var totalMemory = GC.GetTotalMemory(false);
            if (totalMemory > totalMemoryThreshold)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                if (waitForPendingFinalizers)
                {
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        private static IEnumerable<object[]> GetInputFileNames()
        {
            var tqTestFiles = TestSuiteData.GetArcFileNames("tq").Take(10);
            var tqaeTestFiles = TestSuiteData.GetArcFileNames("tqae").Take(10);
            var gdTestFiles = TestSuiteData.GetArcFileNames("gd").Take(10);
            return tqTestFiles
                .Concat(tqaeTestFiles)
                .Concat(gdTestFiles)
                .Select(x => new object[] { x });
        }
    }
}
