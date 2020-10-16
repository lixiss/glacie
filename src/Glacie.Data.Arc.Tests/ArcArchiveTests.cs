using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Glacie.ChecksumAlgorithms;
using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;
using Xunit;

namespace Glacie.Data.Arc
{
    // TODO: (High) (Arc/Arz) Current implementations assume what Stream.Read
    // reads exact number of bytes requested and never return fewer. Use stream
    // wrapper in tests which may return less bytes than requested, to ensure
    // what code is correct (which is not correct).

    [Trait("Category", "ARC")]
    public class ArcArchiveTests
    {
        #region Open

        [Theory, MemberData(nameof(GetData), parameters: 0)]
        public void OpenArc0(string arcFileName, ArcFileFormat expectedLayout)
        {
            using var archive = ArcArchive.Open(arcFileName);
            Assert.Equal(expectedLayout, archive.GetFormat());

            Assert.Equal(0, archive.Count);
            AssertArchiveInvariants(archive);
        }

        [Theory, MemberData(nameof(GetData), parameters: 1)]
        public void OpenArc1(string arcFileName, ArcFileFormat expectedLayout)
        {
            using var archive = ArcArchive.Open(arcFileName);
            Assert.Equal(expectedLayout, archive.GetFormat());

            Assert.Equal(1, archive.Count);
            AssertArchiveInvariants(archive);

            var entry1 = archive.Get("data/empty-file.bin");
            Assert.Equal("data/empty-file.bin", entry1.Name);
            Assert.Equal(0, (long)entry1.Length);
            Assert.Equal(0, (long)entry1.CompressedLength);
            Assert.Equal(1u, entry1.Hash);
            Assert.Equal(1, entry1.EntryType);
            AssertReadEntryAndVerifyHash(entry1);
            AssertReadEntryAndContent(TestData.DataEmptyFileBin, entry1);
        }

        [Theory, MemberData(nameof(GetData), parameters: 2)]
        public void OpenArc2(string arcFileName, ArcFileFormat expectedLayout)
        {
            using var archive = ArcArchive.Open(arcFileName);
            Assert.Equal(expectedLayout, archive.GetFormat());

            Assert.Equal(1, archive.Count);
            AssertArchiveInvariants(archive);

            var entry1 = archive.Get("data/small-file.bin");
            Assert.Equal("data/small-file.bin", entry1.Name);
            Assert.Equal(13, (long)entry1.Length);
            Assert.Equal(13, (long)entry1.CompressedLength);
            Assert.Equal(530449514u, entry1.Hash);
            Assert.Equal(1, entry1.EntryType);
            AssertReadEntryAndVerifyHash(entry1);
            AssertReadEntryAndContent(TestData.DataSmallFileBin, entry1);
        }

        [Theory, MemberData(nameof(GetData), parameters: 3)]
        public void OpenArc3(string arcFileName, ArcFileFormat expectedLayout)
        {
            using var archive = ArcArchive.Open(arcFileName);
            Assert.Equal(expectedLayout, archive.GetFormat());

            Assert.Equal(2, archive.Count);
            AssertArchiveInvariants(archive);

            // This test also ensures what entry names stored in lower case
            // and use forward slash for path separation.
            var entry1 = archive.Get("data/tq-archivetool-help.bin");
            Assert.Equal("data/tq-archivetool-help.bin", entry1.Name);
            Assert.Equal(1070, (long)entry1.Length);
            Assert.True(entry1.CompressedLength < entry1.Length);
            Assert.Equal(2138071381u, entry1.Hash);
            Assert.Equal(3, entry1.EntryType);
            AssertReadEntryAndVerifyHash(entry1);
            AssertReadEntryAndContent(TestData.DataTQArchiveToolHelpBin, entry1);

            var entry2 = archive.Get("data/gd-archivetool-help.bin");
            Assert.Equal("data/gd-archivetool-help.bin", entry2.Name);
            Assert.Equal(681, (long)entry2.Length);
            Assert.True(entry1.CompressedLength < entry1.Length);
            Assert.Equal(1145627443u, entry2.Hash);
            Assert.Equal(3, entry2.EntryType);
            AssertReadEntryAndVerifyHash(entry2);
            AssertReadEntryAndContent(TestData.DataGDArchiveToolHelpBin, entry2);
        }

        [Theory, MemberData(nameof(GetData), parameters: 4)]
        public void OpenArc4(string arcFileName, ArcFileFormat expectedLayout)
        {
            using var archive = ArcArchive.Open(arcFileName);
            Assert.Equal(expectedLayout, archive.GetFormat());

            Assert.Equal(1, archive.Count);
            AssertArchiveInvariants(archive);

            var entry1 = archive.Get("data/tq-archivetool-help.bin");
            Assert.Equal(1070, (long)entry1.Length);
            Assert.True(entry1.CompressedLength < entry1.Length);
            Assert.Equal(2138071381u, entry1.Hash);
            Assert.Equal(3, entry1.EntryType);
            AssertReadEntryAndVerifyHash(entry1);
            AssertReadEntryAndContent(TestData.DataTQArchiveToolHelpBin, entry1);

            // TODO: (Test) (ArcArchive) check about removed files / e.g. file is not compacted - need API
        }

        [Fact]
        public void OpenArcTq5()
        {
            // TODO: (Test) (ArcArchive) This test doesn't cover GD's case... we need GD file with chunks in store mode.

            using var archive = ArcArchive.Open(TestData.Tq5Arc);
            Assert.Equal(ArcFileFormat.FromVersion(1), archive.GetFormat());

            Assert.Equal(1, archive.Count);
            AssertArchiveInvariants(archive);

            var entry1 = archive.Get("data/tokens.bin");
            Assert.Equal(39, (long)entry1.Length);
            Assert.Equal(entry1.CompressedLength, entry1.Length);
            Assert.Equal(2235238612u, entry1.Hash);
            Assert.Equal(3, entry1.EntryType);

            var chunkInfo = entry1.GetChunkInfo();
            Assert.Equal(1, chunkInfo.Count);
            Assert.True(chunkInfo[0].MaybeStore);

            AssertReadEntryAndVerifyHash(entry1);
            AssertReadEntryAndContent(TestData.DataTokensBin, entry1);
        }

        // TODO: (ArcArchiveTests) Create tests which require test suite. They are might be slow.
        // TODO: Can we create smaller files "3gb.arc" files?
        [Fact(Skip = "LongRunning"), Trait("Category", "TestSuite")]
        public void OpenArc3GiB()
        {
            using var archive = ArcArchive.Open(TestDataUtilities.GetPath(@"g:\glacie\glacie-test-suite\arc\tq-3gb.arc"));
            var entry = archive.Get("data/3gb.bin");
            Assert.Equal((long)3 * 1024 * 1024 * 1024, entry.Length);
            Assert.Equal(92189703u, entry.Hash);
            AssertReadEntryAndVerifyHash(entry);
        }

        #endregion

        #region GetEntries

        [Fact]
        public void GetEntries()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);

            var actualEntryNames = archive.SelectAll().Select(x => x.Name).ToList();
            Assert.Equal(2, actualEntryNames.Count);
            Assert.Contains("data/tq-archivetool-help.bin", actualEntryNames);
            Assert.Contains("data/gd-archivetool-help.bin", actualEntryNames);
        }

        #endregion

        #region GetEntry

        [Fact]
        public void GetEntryExisting()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);

            var entry1 = archive.Get("data/tq-archivetool-help.bin");
            var entry2 = archive.Get("data/gd-archivetool-help.bin");
        }

        [Fact]
        public void GetEntryNotExisting()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);

            AssertArcError("EntryNotFound", () => archive.Get("some-unique-name"));
        }

        [Fact]
        public void EntryNamesAreNotNormalized()
        {
            // TODO: (Medium) (Decision) Name/Path normalization

            using var archive = ArcArchive.Open(TestData.Tq3Arc);

            // Names are not normalized
            AssertArcError("EntryNotFound", () => archive.Get("data/TQ-ArchiveTool-help.bin"));
            AssertArcError("EntryNotFound", () => archive.Get("data/GD-ArchiveTool-help.bin"));

            // Path separators also preserved not normalized
            AssertArcError("EntryNotFound", () => archive.Get("data\\tq-archivetool-help.bin"));
            AssertArcError("EntryNotFound", () => archive.Get("data\\gd-archivetool-help.bin"));
        }

        #endregion

        #region TryGetEntry

        [Fact]
        public void TryGetEntry()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);

            AssertExistingTryGetEntry("data/tq-archivetool-help.bin");
            AssertExistingTryGetEntry("data/gd-archivetool-help.bin");
            AssertNonExistingTryGetEntry("non-existing");

            void AssertExistingTryGetEntry(string name)
            {
                Assert.True(archive.TryGet(name, out var e));
                Assert.Equal(name, e.Name);
                Assert.True(archive.Exists(name));
            }

            void AssertNonExistingTryGetEntry(string name)
            {
                Assert.False(archive.TryGet(name, out var x));
            }
        }
        #endregion

        #region GetEntryOrNull

        [Fact]
        public void GetEntryOrNull()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);

            AssertExistingTryGetEntry("data/tq-archivetool-help.bin");
            AssertExistingTryGetEntry("data/gd-archivetool-help.bin");
            AssertNonExistingTryGetEntry("non-existing");

            void AssertExistingTryGetEntry(string name)
            {
                var e = archive.GetOrDefault(name);
                Assert.NotNull(e);
                if (e != null)
                {
                    Assert.Equal(name, e.Value.Name);
                }
            }

            void AssertNonExistingTryGetEntry(string name)
            {
                var e = archive.GetOrDefault(name);
                Assert.Null(e);
            }
        }

        #endregion

        #region Dispose

        [Fact]
        public void Disposed()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);
            var entry1 = archive.Get("data/tq-archivetool-help.bin");
            archive.Dispose();

            Assert.Throws<ObjectDisposedException>(() => archive.Count);
            Assert.Throws<ObjectDisposedException>(() => archive.SelectAll());
            Assert.Throws<ObjectDisposedException>(() => archive.Get("abc"));
            Assert.Throws<ObjectDisposedException>(() => archive.GetOrDefault("abc"));
            Assert.Throws<ObjectDisposedException>(() => archive.GetFormat());
            Assert.Throws<ObjectDisposedException>(() => archive.TryGet("abc", out var _));
            Assert.Throws<ObjectDisposedException>(() => archive.Exists("abc"));
            Assert.Throws<ObjectDisposedException>(() => archive.Add("abc"));

            Assert.Throws<ObjectDisposedException>(() => entry1.Name);
            Assert.Throws<ObjectDisposedException>(() => entry1.Length);
            Assert.Throws<ObjectDisposedException>(() => entry1.CompressedLength);
            Assert.Throws<ObjectDisposedException>(() => entry1.Hash);
            Assert.Throws<ObjectDisposedException>(() => entry1.Timestamp);
            Assert.Throws<ObjectDisposedException>(() => entry1.LastWriteTime);
            Assert.Throws<ObjectDisposedException>(() => entry1.EntryType);
            Assert.Throws<ObjectDisposedException>(() => entry1.GetChunkInfo());
            Assert.Throws<ObjectDisposedException>(() => entry1.Open());
            Assert.Throws<ObjectDisposedException>(() => entry1.OpenWrite());
            Assert.Throws<ObjectDisposedException>(() => entry1.Remove());
        }

        [Fact]
        public void DisposedWithReadingStream()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);
            var entry = archive.Get("data/tq-archivetool-help.bin");
            using var entryStream = entry.Open();
            Assert.Throws<InvalidOperationException>(() => archive.Dispose());
            Assert.Throws<ObjectDisposedException>(() => entryStream.Dispose());
        }

        [Fact]
        public void DisposedWithWritingStream()
        {
            using var archiveStream = new MemoryStream();
            using var archive = ArcArchive.Open(archiveStream,
                new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Create,
                    Format = ArcFileFormat.FromVersion(1)
                });

            var entryName = "entry-name";
            var entry = archive.Add(entryName);
            using var entryStream = entry.OpenWrite(CompressionLevel.NoCompression);
            Assert.Throws<InvalidOperationException>(() => archive.Dispose());
            Assert.Throws<ObjectDisposedException>(() => entryStream.Dispose());
        }

        #endregion

        #region Multiple Stream Readers Are Allowed

        [Fact]
        public void ReadModeOpenMoreThanOneStreamIsAllowed()
        {
            using var archive = ArcArchive.Open(TestData.Tq3Arc);

            var entry1 = archive.Get("data/tq-archivetool-help.bin");
            var entry2 = archive.Get("data/gd-archivetool-help.bin");

            using var entry1Stream = entry1.Open();
            using var entry2Stream = entry2.Open();
        }

        #endregion

        #region Create New Archive & Entries

        [Fact]
        public void CreateRequiresLayout()
        {
            using var archiveStream = new MemoryStream();
            Assert.Throws<ArgumentException>(() =>
            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions { Mode = ArcArchiveMode.Create });
            });
        }

        [Fact]
        public void CreateNewEmptyArchive()
        {
            using var archiveStream = new MemoryStream();
            var options = new ArcArchiveOptions
            {
                Mode = ArcArchiveMode.Create,
                Format = ArcFileFormat.FromVersion(1)
            };
            using var archive = ArcArchive.Open(archiveStream, options);
            archive.Dispose();

            Assert.Equal(options.HeaderAreaLength ?? 2048, archiveStream.Length);

            using var oArchive = ArcArchive.Open(archiveStream);
            Assert.Equal(0, oArchive.Count);
            AssertArchiveInvariants(oArchive);
        }

        [Fact]
        public void CreateNewStoreEntry()
        {
            using var archiveStream = new MemoryStream();
            using var entryContentStream = new MemoryStream();
            {
                using var entryStreamWriter = new StreamWriter(entryContentStream, Encoding.ASCII, leaveOpen: true);
                entryStreamWriter.Write("Hello, World!");
            }
            var entryName = "entry-name";

            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Create,
                        Format = ArcFileFormat.FromVersion(1)
                    });

                var entry = archive.Add(entryName);

                // Name accessible immediately.
                Assert.Equal(entryName, entry.Name);

                // TODO: Ensure rest properties is not yet accessible.
                Assert.Throws<InvalidOperationException>(() => entry.Length);
                Assert.Throws<InvalidOperationException>(() => entry.CompressedLength);
                Assert.Throws<InvalidOperationException>(() => entry.Hash);
                Assert.Throws<InvalidOperationException>(() => entry.Timestamp);
                Assert.Throws<InvalidOperationException>(() => entry.LastWriteTime);
                Assert.Throws<InvalidOperationException>(() => entry.EntryType);
                Assert.Throws<InvalidOperationException>(() => entry.GetChunkInfo());
                Assert.Throws<InvalidOperationException>(() => entry.Open());
                Assert.Throws<InvalidOperationException>(() => entry.Remove());

                {
                    using var entryStream = entry.OpenWrite(CompressionLevel.NoCompression);
                    entryContentStream.Position = 0;
                    entryContentStream.CopyTo(entryStream);
                }

                // Rest properties should be accessible when entry written.
                Assert.Equal(entryContentStream.Length, (long)entry.Length);
                Assert.Equal(entryContentStream.Length, (long)entry.CompressedLength);
                Assert.Equal(530449514u, entry.Hash);
                Assert.True((DateTimeOffset.UtcNow - entry.LastWriteTime).TotalMilliseconds < 2000);
                Assert.Equal(1, entry.EntryType);

                // Open, Remove should throw in Create mode
                // OpenWrite called second time should throw in Create mode
                Assert.Throws<InvalidOperationException>(() => entry.Open());
                Assert.Throws<InvalidOperationException>(() => entry.Remove());
                Assert.Throws<InvalidOperationException>(() => entry.OpenWrite());
            }

            using var oArchive = ArcArchive.Open(archiveStream);
            Assert.Equal(1, oArchive.Count);
            AssertArchiveInvariants(oArchive);
            var oEntry = oArchive.Get(entryName);
            using var oEntryStream = oEntry.Open();
            entryContentStream.Position = 0;
            AssertStreamContent(entryContentStream, oEntryStream);
        }

        [Fact]
        public void CreateNewEntryChunkedStore()
        {
            using var archiveStream = new MemoryStream();
            using var entryContentStream = new MemoryStream();
            {
                using var entryStreamWriter = new BinaryWriter(entryContentStream, Encoding.ASCII, leaveOpen: true);
                for (var i = 0; i < 240; i++)
                    entryStreamWriter.Write((sbyte)i);
            }
            var expectedHash = new Adler32().ComputeHash(entryContentStream.ToArray());
            var entryName = "entry-name";

            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Create,
                        Format = ArcFileFormat.FromVersion(3),
                        ChunkLength = 16,
                    });

                var entry = archive.Add(entryName);

                {
                    using var entryStream = entry.OpenWrite(CompressionLevel.Fastest);
                    entryContentStream.Position = 0;
                    entryContentStream.CopyTo(entryStream);
                }

                // Rest properties should be accessible when entry written.
                Assert.Equal(entryContentStream.Length, entry.Length);
                Assert.True((long)entry.CompressedLength <= entry.Length);
                Assert.Equal(expectedHash, entry.Hash);
                Assert.Equal(3, entry.EntryType);
            }

            using var oArchive = ArcArchive.Open(archiveStream);
            Assert.Equal(1, oArchive.Count);
            AssertArchiveInvariants(oArchive);
            var oEntry = oArchive.Get(entryName);
            using var oEntryStream = oEntry.Open();
            entryContentStream.Position = 0;
            AssertStreamContent(entryContentStream, oEntryStream);

            Assert.Equal(15, oEntry.GetChunkInfo().Count);
        }

        [Fact]
        public void CreateNewEntryChunkedDowngradeToStore()
        {
            using var archiveStream = new MemoryStream();
            using var entryContentStream = new MemoryStream();
            {
                using var entryStreamWriter = new BinaryWriter(entryContentStream, Encoding.ASCII, leaveOpen: true);
                for (var i = 0; i < 240; i++)
                    entryStreamWriter.Write((sbyte)i);
            }
            var expectedHash = new Adler32().ComputeHash(entryContentStream.ToArray());
            var entryName = "entry-name";

            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Create,
                        Format = ArcFileFormat.FromVersion(3),
                        ChunkLength = 256,
                    });

                var entry = archive.Add(entryName);

                {
                    using var entryStream = entry.OpenWrite(CompressionLevel.Fastest);
                    entryContentStream.Position = 0;
                    entryContentStream.CopyTo(entryStream);
                }

                // Rest properties should be accessible when entry written.
                Assert.Equal(entryContentStream.Length, entry.Length);
                Assert.Equal(entryContentStream.Length, entry.CompressedLength);
                Assert.Equal(expectedHash, entry.Hash);
                Assert.Equal(1, entry.EntryType);
            }

            using var oArchive = ArcArchive.Open(archiveStream);
            Assert.Equal(1, oArchive.Count);
            AssertArchiveInvariants(oArchive);
            var oEntry = oArchive.Get(entryName);
            using var oEntryStream = oEntry.Open();
            entryContentStream.Position = 0;
            AssertStreamContent(entryContentStream, oEntryStream);

            Assert.Equal(0, oEntry.GetChunkInfo().Count);
        }

        [Fact]
        public void CreateNewEntryAlreadyExist()
        {
            using var archiveStream = new MemoryStream();
            using var archive = ArcArchive.Open(archiveStream,
                new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Create,
                    Format = ArcFileFormat.FromVersion(1)
                });

            var entryName = "entry-name-1";
            var entry = archive.Add(entryName);
            Assert.Equal(entryName, entry.Name);
            using var entryStream = entry.OpenWrite(CompressionLevel.NoCompression);

            AssertArcError("EntryAlreadyExist", () => archive.Add(entryName));
        }

        [Fact]
        public void CreatedEntryShouldBeWritten()
        {
            using var archiveStream = new MemoryStream();
            var archive = ArcArchive.Open(archiveStream,
                new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Create,
                    Format = ArcFileFormat.FromVersion(1)
                });

            var entryName = "entry-name";
            var entry = archive.Add(entryName);

            // We write nothing to new entry - there is logical error.
            Assert.Throws<InvalidOperationException>(() => archive.Dispose());
        }

        #endregion

        #region Remove (Update mode)

        [Fact]
        public void RemoveShouldThrowInReadMode()
        {
            var inputBytes = File.ReadAllBytes(TestData.Tq3Arc);
            using var inputStream = new MemoryStream(inputBytes.Length);
            inputStream.Write(inputBytes);

            using var inputArchive = ArcArchive.Open(inputStream,
                new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Read,
                });

            var entry = inputArchive.Get("data/tq-archivetool-help.bin");

            Assert.Throws<InvalidOperationException>(() => entry.Remove());
        }

        [Fact]
        public void RemoveShouldThrowInCreateMode()
        {
            using var inputStream = new MemoryStream();
            using var inputArchive = ArcArchive.Open(inputStream,
                new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Create,
                    Format = ArcFileFormat.FromVersion(1),
                });

            var entry = inputArchive.Add("some-entry");
            Assert.Throws<InvalidOperationException>(() => entry.Remove());

            { using var entryStream = entry.OpenWrite(); }
            Assert.Throws<InvalidOperationException>(() => entry.Remove());
        }

        [Fact]
        public void Remove()
        {
            var inputBytes = File.ReadAllBytes(TestData.Tq3Arc);
            using var inputStream = new MemoryStream(inputBytes.Length);
            inputStream.Write(inputBytes);

            {
                using var inputArchive = ArcArchive.Open(inputStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Update,
                    });

                var entry = inputArchive.Get("data/tq-archivetool-help.bin");

                entry.Remove();

                Assert.False(inputArchive.TryGet("data/tq-archivetool-help.bin", out var _));

                // Access to removed entry is invalid.
                Assert.Throws<InvalidOperationException>(() => entry.Length);
                Assert.Throws<InvalidOperationException>(() => entry.CompressedLength);
                Assert.Throws<InvalidOperationException>(() => entry.Hash);
                Assert.Throws<InvalidOperationException>(() => entry.Timestamp);
                Assert.Throws<InvalidOperationException>(() => entry.LastWriteTime);
                Assert.Throws<InvalidOperationException>(() => entry.EntryType);
                Assert.Throws<InvalidOperationException>(() => entry.GetChunkInfo());
                Assert.Throws<InvalidOperationException>(() => entry.Open());
                Assert.Throws<InvalidOperationException>(() => entry.OpenWrite());
                Assert.Throws<InvalidOperationException>(() => entry.Remove());
            }

            using var oArchive = ArcArchive.Open(inputStream);
            Assert.Equal(1, oArchive.Count);
            AssertArchiveInvariants(oArchive);

            var li = oArchive.GetLayoutInfo();
            Assert.Equal(1, li.FreeSegmentCount);
        }

        #endregion

        #region Update Archive & Entries

        [Fact]
        public void UpdateShouldThrowWhenWritingAlreadyOpenedEntry()
        {
            var inputBytes = File.ReadAllBytes(TestData.Tq3Arc);
            using var inputStream = new MemoryStream(inputBytes.Length);
            inputStream.Write(inputBytes);

            {
                using var inputArchive = ArcArchive.Open(inputStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Update,
                    });

                var entry = inputArchive.Get("data/tq-archivetool-help.bin");

                using var readStream = entry.Open();

                Assert.Throws<InvalidOperationException>(() => entry.OpenWrite());
            }
        }

        [Fact]
        public void UpdateShouldThrowWhenReadAlreadyWritingEntry()
        {
            var inputBytes = File.ReadAllBytes(TestData.Tq3Arc);
            using var inputStream = new MemoryStream(inputBytes.Length);
            inputStream.Write(inputBytes);

            {
                using var inputArchive = ArcArchive.Open(inputStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Update,
                    });

                var entry = inputArchive.Get("data/tq-archivetool-help.bin");

                using var writeStream = entry.OpenWrite();
                Assert.Throws<InvalidOperationException>(() => entry.Open());
            }
        }

        [Fact]
        public void Update()
        {
            var archiveBytes = File.ReadAllBytes(TestData.Gd3Arc);
            using var archiveStream = new MemoryStream(archiveBytes.Length);
            archiveStream.Write(archiveBytes);

            using var entryContentStream1 = new MemoryStream();
            {
                using var entryStreamWriter = new BinaryWriter(entryContentStream1, Encoding.ASCII, leaveOpen: true);
                for (var i = 0; i < 240; i++)
                    entryStreamWriter.Write((sbyte)i);
            }
            var expectedHash1 = new Adler32().ComputeHash(entryContentStream1.ToArray());
            var entryName1 = "entry-name";

            using var entryContentStream2 = new MemoryStream();
            {
                using var entryStreamWriter = new BinaryWriter(entryContentStream2, Encoding.ASCII, leaveOpen: true);
                for (var i = 0; i < 256; i++)
                    entryStreamWriter.Write((sbyte)(256 - i));
            }
            var expectedHash2 = new Adler32().ComputeHash(entryContentStream2.ToArray());
            var entryName2 = "data/tq-archivetool-help.bin";

            {
                using var archive = ArcArchive.Open(archiveStream,
                    new ArcArchiveOptions
                    {
                        Mode = ArcArchiveMode.Update,
                        ChunkLength = 16,
                    });

                var entry1 = archive.Add(entryName1);
                {
                    using var entryStream = entry1.OpenWrite(CompressionLevel.Fastest);
                    entryContentStream1.Position = 0;
                    entryContentStream1.CopyTo(entryStream);
                }
                Assert.Equal(entryContentStream1.Length, entry1.Length);
                Assert.True((long)entry1.CompressedLength <= entry1.Length);
                Assert.Equal(expectedHash1, entry1.Hash);
                Assert.Equal(3, entry1.EntryType);

                var entry2 = archive.Get(entryName2);
                {
                    using var entryStream = entry2.OpenWrite(CompressionLevel.Fastest);
                    entryContentStream2.Position = 0;
                    entryContentStream2.CopyTo(entryStream);
                }
                Assert.Equal(entryContentStream2.Length, entry2.Length);
                Assert.True((long)entry2.CompressedLength <= entry2.Length);
                Assert.Equal(expectedHash2, entry2.Hash);
                Assert.Equal(3, entry2.EntryType);
            }

            using var oArchive = ArcArchive.Open(archiveStream);
            Assert.Equal(3, oArchive.Count);
            AssertArchiveInvariants(oArchive);
            var oEntry1 = oArchive.Get(entryName1);
            using var oEntryStream1 = oEntry1.Open();
            entryContentStream1.Position = 0;
            AssertStreamContent(entryContentStream1, oEntryStream1);
            Assert.Equal(15, oEntry1.GetChunkInfo().Count);

            var oEntry2 = oArchive.Get(entryName2);
            using var oEntryStream2 = oEntry2.Open();
            entryContentStream2.Position = 0;
            AssertStreamContent(entryContentStream2, oEntryStream2);
            Assert.Equal(16, oEntry2.GetChunkInfo().Count);
        }

        #endregion

        private static void AssertArcError(string expectedErrorCode, Action testCode)
        {
            var ex = Assert.Throws<ArcException>(testCode);
            Assert.Equal(expectedErrorCode, ex.ErrorCode);
        }

        private void AssertArchiveInvariants(ArcArchive archive)
        {
            var count = archive.Count;
            var iteratedCount = archive.SelectAll().Count();
            Assert.Equal(count, iteratedCount);
        }

        private void AssertReadEntryAndVerifyHash(ArcArchiveEntry entry)
        {
            const int BufferSize = 16 * 1024;

            using var stream = entry.Open();
            var buffer = new byte[BufferSize];
            var hash = new Adler32();
            while (true)
            {
                var bytesRead = stream.Read(buffer);
                if (bytesRead == 0) break;
                hash.ComputeHash(buffer, 0, bytesRead);
            }

            Assert.Equal(entry.Hash, hash.Hash);
        }

        private void AssertReadEntryAndContent(string expectedFile, ArcArchiveEntry entry)
        {
            var expectedBytes = File.ReadAllBytes(expectedFile);
            Assert.Equal((long)expectedBytes.Length, entry.Length);

            {
                using var actualStream = entry.Open();
                using var expectedStream = new MemoryStream(expectedBytes, false);
                AssertStreamContent(expectedStream, actualStream, bufferSize: 1);
            }

            {
                using var actualStream = entry.Open();
                using var expectedStream = new MemoryStream(expectedBytes, false);
                AssertStreamContent(expectedStream, actualStream, bufferSize: 3);
            }

            {
                using var actualStream = entry.Open();
                using var expectedStream = new MemoryStream(expectedBytes, false);
                AssertStreamContent(expectedStream, actualStream, bufferSize: 1024);
            }

            {
                using var actualStream = entry.Open();
                using var expectedStream = new MemoryStream(expectedBytes, false);
                Assert.True(expectedBytes.Length <= 16384);
                AssertStreamContent(expectedStream, actualStream, bufferSize: 16384);
            }
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

        private static IEnumerable<object[]> GetData(int arcFileNumber)
        {
            switch (arcFileNumber)
            {
                case 0:
                    yield return new object[] { TestData.Tq0Arc, ArcFileFormat.FromVersion(1) };
                    yield return new object[] { TestData.Gd0Arc, ArcFileFormat.FromVersion(3) };
                    break;

                case 1:
                    yield return new object[] { TestData.Tq1Arc, ArcFileFormat.FromVersion(1) };
                    yield return new object[] { TestData.Gd1Arc, ArcFileFormat.FromVersion(3) };
                    break;

                case 2:
                    yield return new object[] { TestData.Tq2Arc, ArcFileFormat.FromVersion(1) };
                    yield return new object[] { TestData.Gd2Arc, ArcFileFormat.FromVersion(3) };
                    break;

                case 3:
                    yield return new object[] { TestData.Tq3Arc, ArcFileFormat.FromVersion(1) };
                    yield return new object[] { TestData.Gd3Arc, ArcFileFormat.FromVersion(3) };
                    break;

                case 4:
                    yield return new object[] { TestData.Tq4Arc, ArcFileFormat.FromVersion(1) };
                    yield return new object[] { TestData.Gd4Arc, ArcFileFormat.FromVersion(3) };
                    break;

                case 5:
                    yield return new object[] { TestData.Tq5Arc, ArcFileFormat.FromVersion(1) };
                    break;

                default:
                    throw Error.Argument(nameof(arcFileNumber));
            }
        }
    }
}
