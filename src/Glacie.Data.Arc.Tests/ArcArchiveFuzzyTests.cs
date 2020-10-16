using System;
using System.Collections.Generic;
using System.IO;

using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;

using Xunit;
using Xunit.Abstractions;

namespace Glacie.Data.Arc
{
    [Trait("Category", "ARC")]
    public class ArcArchiveFuzzTests
    {
        private const int ChunkLength = 1024;

        private const int NumberOfUpdates = 20;
        private const int MaxNumberOfEntries = 300;

        private const int MaxEntryLength = 4 * ChunkLength;

        private readonly Random _rng = new Random(123);
        private readonly Dictionary<string, byte[]> _entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        private readonly ITestOutputHelper Output;

        public ArcArchiveFuzzTests(ITestOutputHelper output)
        {
            Output = output;
        }

        [Fact]
        public void FuzzWrites()
        {
            // TODO: go over different layouts too...
            using var archiveStream = new MemoryStream();

            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Create,
                        Format = ArcFileFormat.FromVersion(1),
                        ChunkLength = ChunkLength,
                    });
                CreateFuzzArchive(archive);
            }

            var canCompact = false;
            var canDefragment = false;
            for (var i = 0; i < NumberOfUpdates; i++)
            {
                var isSafeWrite = _rng.Next() % 2 == 0;
                var doCompact = _rng.Next() % 2 == 0;
                var doRepack = _rng.Next() % 3 == 0;
                var doDefragment = _rng.Next() % 4 == 0;

                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Update,
                        SafeWrite = isSafeWrite,
                        ChunkLength = ChunkLength,
                    });
                UpdateFuzzArchive(archive);

                var layoutInfo = archive.GetLayoutInfo();
                canDefragment |= layoutInfo.CanDefragment;
                canCompact |= layoutInfo.CanCompact;

                if (doDefragment)
                {
                    archive.Defragment();
                }

                if (!isSafeWrite && doCompact)
                {
                    archive.Compact();
                }

                if (!isSafeWrite && doRepack)
                {
                    archive.Repack(CompressionLevel.Maximum);
                }
            }

            Assert.True(canCompact);
            Assert.True(canDefragment);

            // Compact
            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Update,
                        SafeWrite = false,
                        ChunkLength = ChunkLength,
                    });
                archive.Compact();
            }

            // Validate archive
            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Read,
                    });
                ValidateFuzzArchive(archive);

                // 167624128 without reusing blocks
                var layoutInfo = archive.GetLayoutInfo();
                Output.WriteLine("         EntryCount: {0}", layoutInfo.EntryCount);
                Output.WriteLine("  RemovedEntryCount: {0}", layoutInfo.RemovedEntryCount);
                Output.WriteLine("         ChunkCount: {0}", layoutInfo.ChunkCount);
                Output.WriteLine("     LiveChunkCount: {0}", layoutInfo.LiveChunkCount);
                Output.WriteLine("UnorderedChunkCount: {0}", layoutInfo.UnorderedChunkCount);
                Output.WriteLine("  FreeFragmentCount: {0}", layoutInfo.FreeSegmentCount);
                Output.WriteLine("  FreeFragmentBytes: {0:N0}", layoutInfo.FreeSegmentBytes);
                Output.WriteLine("");
                Output.WriteLine("        Can Compact: {0}", layoutInfo.CanCompact);
                Output.WriteLine("     Can Defragment: {0}", layoutInfo.CanDefragment);
            }
        }

        private void CreateFuzzArchive(ArcArchive archive)
        {
            for (var i = 0; i < MaxNumberOfEntries; i++)
            {
                var entryName = CreateFuzzEntry();
                var entryData = _entries[entryName];

                if (!archive.Exists(entryName))
                {
                    var entry = archive.Add(entryName);
                    var compressionLevel = CompressionLevel.NoCompression; // (CompressionLevel)_rng.Next(0, 13);
                    using var entryStream = entry.OpenWrite(compressionLevel);
                    entryStream.Write(entryData);
                }
            }
        }

        private void UpdateFuzzArchive(ArcArchive archive)
        {
            var numberOfUpdates = _rng.Next(0, MaxNumberOfEntries);
            for (var i = 0; i < numberOfUpdates; i++)
            {
                var entryName = CreateFuzzEntry();
                if (_rng.Next(0, 2) == 1)
                {
                    _entries[entryName] = CreateFuzzEntryData();
                }

                ArcArchiveEntry entry;
                bool wasCreated;
                if (archive.Exists(entryName))
                {
                    entry = archive.Get(entryName);
                    wasCreated = false;
                }
                else
                {
                    entry = archive.Add(entryName);
                    wasCreated = true;
                }

                var doRemove = _rng.Next(0, 4) == 1;
                if (doRemove && !wasCreated) // TODO: Make CreateEntry and immediately remove possible...?
                {
                    entry.Remove();
                }
                else
                {
                    var compressionLevel = (CompressionLevel)_rng.Next(0, 13);
                    using var entryStream = entry.OpenWrite(compressionLevel);
                    var withSingleCall = _rng.Next() % 2 == 0;
                    if (withSingleCall)
                    {
                        entryStream.Write(_entries[entryName]);
                    }
                    else
                    {
                        ReadOnlySpan<byte> data = _entries[entryName];
                        if (data.Length >= 1)
                        {
                            entryStream.Write(data.Slice(0, 1));

                            if (data.Length > 1)
                            {
                                entryStream.Write(data.Slice(1));
                            }
                        }
                    }
                }
            }
        }

        private void ValidateFuzzArchive(ArcArchive archive)
        {
            foreach (var entry in archive.SelectAll())
            {
                using var entryStream = entry.Open();

                var entryData = _entries[entry.Name];
                AssertStreamContent(
                    new MemoryStream(entryData, false),
                    entryStream);
            }
        }

        private string CreateFuzzEntry()
        {
            var entryNo = _rng.Next(1, MaxNumberOfEntries + 1);
            var entryName = "entry/" + entryNo;
            if (!_entries.ContainsKey(entryName))
            {
                _entries[entryName] = CreateFuzzEntryData();
            }
            return entryName;
        }

        private byte[] CreateFuzzEntryData()
        {
            var length = _rng.Next(0, MaxEntryLength);
            if (_rng.Next() % 3 == 0)
            {
                length = _rng.Next(0, ChunkLength);
            }

            var array = new byte[length];
            var type = _rng.Next(0, 2);
            if (type == 0)
            {
                var fill = (byte)_rng.Next();
                new Span<byte>(array).Fill(fill);
                return array;
            }
            else if (type == 1)
            {
                _rng.NextBytes(array);
                return array;
            }
            else throw Error.Unreachable();
        }

        private void AssertStreamContent(Stream expectedStream, Stream actualStream, int bufferSize = 16384)
        {
            var entryBuffer = new byte[bufferSize];
            var inputBuffer = new byte[bufferSize];
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
                            Assert.True(false, "Content differs.");
                    }
                }
                else
                {
                    // There is possible if stream returns less bytes than requested.
                    throw Error.Unreachable();
                }
            }
        }

    }
}
