using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Glacie.Buffers;
using Glacie.ChecksumAlgorithms;
using Glacie.Collections;
using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;

using IO = System.IO;

// TODO: (Medium) (ArcArchive) It sometimes can't compact because... flush is not called? Grab actual TOC offset, and desired / "freeOffset".
// Just repeatable gx-arc replace current show constant archive grow, but it should not. This currently already partially mitigated.

// TODO: (Low) (ArcArchive) Looks like we may want throw ArcException instead of InvalidOperation?

// TODO: (Medium) (API) (Glacie) Because Arz/Arc classes are prefixed, move them into Glacie.Data namespaces (but keep in own assemblies)?
namespace Glacie.Data.Arc
{
    using EntryMap = Dictionary<string, arc_entry_id>;

    public sealed partial class ArcArchive : IDisposable
    {
        #region Factory

        public static ArcArchive Open(IO.Stream stream,
            ArcArchiveMode mode = ArcArchiveMode.Read,
            bool leaveOpen = true)
            => new ArcArchive(stream, leaveOpen: leaveOpen, options: new ArcArchiveOptions { Mode = mode });

        public static ArcArchive Open(IO.Stream stream,
            ArcArchiveOptions options,
            bool leaveOpen = true)
        {
            Check.Argument.NotNull(options, nameof(options));
            return new ArcArchive(stream, leaveOpen: leaveOpen, options: options);
        }

        public static ArcArchive Open(string path, ArcArchiveMode mode = ArcArchiveMode.Read)
            => Open(path, options: new ArcArchiveOptions { Mode = mode });

        public static ArcArchive Open(string path,
            ArcArchiveOptions options)
        {
            Check.Argument.NotNull(options, nameof(options));

            // Validate options to fail before new file will be created.
            if (options.Mode == ArcArchiveMode.Create)
            {
                ValidateOptions(options);
            }

            var inputStream = OpenStream(path, options.Mode);
            try
            {
                var result = new ArcArchive(inputStream, leaveOpen: false, options: options);
                inputStream = null;
                return result;
            }
            finally
            {
                inputStream?.Dispose();
            }
        }

        #endregion

        private const uint ArcHeaderMagic = 0x00435241;
        private const int WritingStoreBufferSize = 8 * 1024;
        private const int DefaultChunkLength = 256 * 1024;

        // If chunk compression result in pathetic saves,
        // then try emit chunk as uncompressed.
        // TODO: (VeryLow) (ArcArchive) CompressedChunkThreshold is have more
        // sense in LZ4, but need choose actual sensible threshold.
        private const int CompressedChunkThreshold = 2;

        private IO.Stream _stream;

        // Flags
        private bool _leaveOpen;
        private bool _disposed;
        private bool _hasEntryTable;
        private bool _hasChunkTable;
        private bool _wasModified;

        // options
        private ArcArchiveMode _mode;

        // Data which may be read from file, and should be pre-exited in create mode.
        private ArcFileHeader _header;
        private ReferenceValueList<Entry> _entryTable;
        private ReferenceValueList<ArcEntryChunk> _chunkTable;
        private EntryMap? _entryMap;

        // options
        private ArcFileFormat _format;
        private CompressionLevel _compressionLevel;
        private bool _safeWrite;
        private int _headerAreaLength;
        private int _chunkLength;
        private bool? _useLibDeflate;

        // cached decoder and encoder
        private Decoder? _decoder;
        private Encoder? _encoder;
        private CompressionLevel _encoderCompressionLevel = CompressionLevel.NoCompression;

        // track active streams
        private int _numberOfReadLocks;
        private ArcEntryStream? _activeWritingStream;

        // free segment allocator
        private long _freeOffset;
        private ReferenceValueList<FileSegment> _freeSegments;
        private bool _hasFreeSegments;

        // accumulated data for writing
        private ArcFileEntryType _wEntryType;
        private uint _wFileOffset;
        private uint _wHash;
        private uint _wLength;
        private uint _wCompressedLength;
        private ReferenceValueList<ArcEntryChunk> _wChunks;
        private bool _wasStoreBlockAllocated;

        private ArcArchive(IO.Stream stream, bool leaveOpen, ArcArchiveOptions options)
        {
            Check.Argument.NotNull(stream, nameof(stream));
            Check.Argument.NotNull(options, nameof(options));

            ValidateOptions(options);

            _mode = options.Mode;
            ValidateStreamAndMode(stream, _mode);
            _format = options.Format;
            _compressionLevel = options.CompressionLevel;
            _safeWrite = options.SafeWrite;
            _headerAreaLength = options.HeaderAreaLength ?? 2048;
            _chunkLength = options.ChunkLength ?? DefaultChunkLength;
            _useLibDeflate = options.UseLibDeflate;

            if (_headerAreaLength < ArcFileHeader.Size)
            {
                throw Error.Argument(nameof(options), "Header area length too short.");
            }
            else if (_headerAreaLength > 4096)
            {
                throw Error.Argument(nameof(options), "Header area length too long.");
            }

            _stream = stream;
            _leaveOpen = leaveOpen;

            if (_mode == ArcArchiveMode.Create)
            {
                CreateHeaderAndCatalog();
            }
            else
            {
                ReadAndCheckHeader();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (_numberOfReadLocks > 0
                        || _activeWritingStream != null)
                    {
                        throw Error.InvalidOperation("Active streams should be disposed before archive is disposed.");
                    }

                    if (_mode != ArcArchiveMode.Read)
                    {
                        Flush();
                    }
                }
                finally
                {
                    if (!_leaveOpen)
                    {
                        _stream?.Dispose();
                        _stream = null!;
                    }

                    _entryTable.Dispose();
                    _chunkTable.Dispose();
                    _entryMap = null!;

                    _decoder?.Dispose();
                    _decoder = null;

                    _encoder?.Dispose();
                    _encoder = null;

                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Returns file format used for this archive.
        /// There is infrastructure method, and not intended to use directly.
        /// </summary>
        public ArcFileFormat GetFormat()
        {
            // TODO: Create GetContext() method which will report Path and Format, similar to ARZ.

            ThrowIfDisposed();
            return _format;
        }

        public int Count
        {
            get
            {
                ThrowIfDisposed();
                ReadEntriesIfNeed();
                return _entryMap.Count;
            }
        }

        public bool Modified
        {
            get
            {
                ThrowIfDisposed();
                return _wasModified;
            }
        }

        public IEnumerable<ArcArchiveEntry> SelectAll()
        {
            ThrowIfDisposed();
            ReadEntriesIfNeed();

            foreach (var entryId in _entryMap!.Values)
            {
                yield return new ArcArchiveEntry(this, entryId);
            }
        }

        // TODO: (Low) (ArcArchive) Naming consistency: GetEntry -> Get, TryGetEntry -> TryGet, GetEntryOrNull -> GetOrDefault, CreateEntry->Add.
        public ArcArchiveEntry Get(string name)
        {
            ThrowIfDisposed();
            ReadEntriesIfNeed();
            if (_entryMap.TryGetValue(name, out var entryId))
            {
                return new ArcArchiveEntry(this, entryId);
            }
            else throw ArcError.EntryNotFound(name);
        }

        public bool TryGet(string name, out ArcArchiveEntry entry)
        {
            ThrowIfDisposed();
            ReadEntriesIfNeed();
            if (_entryMap.TryGetValue(name, out var entryId))
            {
                entry = new ArcArchiveEntry(this, entryId);
                return true;
            }
            else
            {
                entry = default;
                return false;
            }
        }

        public ArcArchiveEntry? GetOrDefault(string name)
        {
            ThrowIfDisposed();
            ReadEntriesIfNeed();
            if (_entryMap.TryGetValue(name, out var entryId))
            {
                return new ArcArchiveEntry(this, entryId);
            }
            else
            {
                return null;
            }
        }

        public bool Exists(string name)
        {
            return TryGet(name, out var _);
        }

        public ArcArchiveEntry Add(string name)
        {
            ThrowIfDisposed();
            if (_mode != ArcArchiveMode.Create && _mode != ArcArchiveMode.Update)
                throw Error.InvalidOperation("Creating new entries is not allowed in current mode.");

            // TODO: (High) (ArcArchive) Validate entry name: relative path segments are not allowed.
            // TODO: (High) (ArcArchive) Normalize entry name. (Lower case it...) Alternatively we might not do it, or have option.

            if (Exists(name)) throw ArcError.EntryAlreadyExist(name);

            var entry = new Entry(name);

            var entryId = (arc_entry_id)_entryTable.Add(in entry);
            var entryMap = GetEntryMap();
            entryMap.Add(name, entryId);

            return new ArcArchiveEntry(this, entryId);
        }

        public ArcLayoutInfo GetLayoutInfo()
        {
            ThrowIfDisposed();
            ReadEntriesIfNeed();
            ReadChunkTableIfNeed();

            var entryCount = _entryTable.Count;
            var removedEntryCount = 0;
            var chunkCount = _chunkTable.Count;
            var liveChunkCount = 0;
            var unorderedChunkCount = 0;

            for (var i = 0; i < _entryTable.Count; i++)
            {
                ref var entry = ref _entryTable[i];

                if (entry.IsRemoved)
                {
                    removedEntryCount++;
                }
                else
                {
                    if (entry.EntryType == ArcFileEntryType.Chunked)
                    {
                        liveChunkCount += entry.ChunkCount;

                        var previousChunk = default(ArcEntryChunk);
                        var lastChunkIndexExclusive = entry.ChunkIndex + entry.ChunkCount;
                        for (var chunkIndex = entry.ChunkIndex; chunkIndex < lastChunkIndexExclusive; chunkIndex++)
                        {
                            ref var chunk = ref _chunkTable[chunkIndex];

                            if (chunkIndex > entry.ChunkIndex)
                            {
                                if (chunk.Offset != (previousChunk.Offset + previousChunk.CompressedLength))
                                {
                                    unorderedChunkCount++;
                                }
                            }
                            previousChunk = chunk;
                        }
                    }
                }
            }

            CalculateFreeSegmentsIfNeed();

            var freeSegmentCount = _freeSegments.Count;
            long freeFragmentBytes = 0;
            for (var i = 0; i < freeSegmentCount; i++)
            {
                ref var freeSegment = ref _freeSegments[i];
                freeFragmentBytes += freeSegment.Length;
            }

            // TODO: There is hack, because tail free segment may not reported.
            if (_freeOffset < _header.ChunkTableOffset)
            {
                var length = _header.ChunkTableOffset - _freeOffset;
                freeFragmentBytes += length;
                freeSegmentCount++;
            }

            return new ArcLayoutInfo
            {
                EntryCount = entryCount,
                RemovedEntryCount = removedEntryCount,

                ChunkCount = chunkCount,
                LiveChunkCount = liveChunkCount,
                UnorderedChunkCount = unorderedChunkCount,

                FreeSegmentCount = freeSegmentCount,
                FreeSegmentBytes = freeFragmentBytes,
            };
        }

        public void Defragment(IIncrementalProgress<long>? progress = null)
        {
            ThrowIfDisposed();
            ThrowInReadMode();

            if (_numberOfReadLocks > 0 || _activeWritingStream != null)
            {
                throw Error.InvalidOperation("Any active streams should be closed before archive can be compacted.");
            }

            DefragmentCore(progress);
        }

        public void Compact(IIncrementalProgress<long>? progress = null)
        {
            ThrowIfDisposed();
            ThrowInReadMode();

            if (_safeWrite) throw Error.InvalidOperation("Compact in safe write mode is not supported.");

            if (_numberOfReadLocks > 0 || _activeWritingStream != null)
            {
                throw Error.InvalidOperation("Any active streams should be closed before archive can be compacted.");
            }

            CalculateFreeSegmentsIfNeed();

            var layoutInfo = GetLayoutInfo();

            CompactCore(
                repack: false,
                compressionLevel: default,
                progress: progress);

            if (layoutInfo.RemovedEntryCount > 0)
            {
                // Removed entries will be removed when archive is flushed.
                _wasModified = true;
            }

            if (layoutInfo.LiveChunkCount < layoutInfo.ChunkCount)
            {
                // Chunk table will be rebuilt when archive is flushed.
                _wasModified = true;
            }
        }

        // TODO: (Low) (ArcArchive) Repack statistics
        public void Repack(CompressionLevel? compressionLevel = null, IIncrementalProgress<long>? progress = null)
        {
            ThrowIfDisposed();
            ThrowInReadMode();

            if (_safeWrite) throw Error.InvalidOperation("Compact in safe write mode is not supported.");

            if (_numberOfReadLocks > 0 || _activeWritingStream != null)
            {
                throw Error.InvalidOperation("Any active streams should be closed before archive can be compacted.");
            }

            CalculateFreeSegmentsIfNeed();

            CompactCore(
                repack: true,
                compressionLevel: compressionLevel ?? _compressionLevel,
                progress: progress);
        }

        #region Entry Internal API

        internal ref Entry GetEntry(arc_entry_id id)
        {
            return ref _entryTable[(int)id];
        }

        internal void SetEntryTimestamp(arc_entry_id entryId, long value)
        {
            ThrowIfDisposed();
            ThrowInReadMode();
            ref var entry = ref GetEntry(entryId);
            entry.SetTimestamp(value);
            _wasModified = true;
        }

        internal IO.Stream OpenReadingStream(arc_entry_id entryId)
        {
            ThrowIfDisposed();
            ThrowInCreateMode();
            // TODO: (Medium) (ArcArchive) Add validation test which read simulataneously stream and check hashes.
            ReadChunkTableIfNeed();

            if (_activeWritingStream != null)
            {
                if (_activeWritingStream.EntryId == entryId)
                {
                    throw Error.InvalidOperation("Can't open entry which currently writing.");
                }
            }

            ref var entry = ref GetEntry(entryId);
            if (entry.IsRemoved)
            {
                throw Error.InvalidOperation("Can't open removed entry.");
            }

            _numberOfReadLocks++;
            entry.EnterReadLock();

            if (entry.EntryType == ArcFileEntryType.Store)
            {
                return new ReadingStoreEntryStream(this,
                    entryId: entryId,
                    length: entry.Length,
                    offset: checked((int)entry.Offset),
                    maxBufferSize: 128 * 1024); // TODO: configure buffer size
            }
            else
            {
                var chunkInfo = GetChunkInfoInternal(ref entry);
                return new ReadingChunkedEntryStream(this,
                    entryId: entryId,
                    length: entry.Length,
                    chunks: chunkInfo);
            }
        }

        internal IO.Stream OpenWritingStream(arc_entry_id entryId, CompressionLevel? compressionLevel)
        {
            ThrowIfDisposed();
            ThrowInReadMode();

            ReadEntriesIfNeed();
            ReadChunkTableIfNeed();
            CalculateFreeSegmentsIfNeed();

            if (_activeWritingStream != null)
            {
                throw Error.InvalidOperation("Only one entry stream may be opened at time.");
            }

            ref var entry = ref GetEntry(entryId);
            if (!entry.IsNew && entry.IsRemoved)
            {
                throw Error.InvalidOperation("Can't open removed entry.");
            }
            if (entry.HasReadLock)
            {
                throw Error.InvalidOperation("Can't open entry for writing, while it already opened for reading.");
            }
            Check.True(!entry.HasWriteLock);
            if (_mode == ArcArchiveMode.Create && entry.Written)
            {
                throw Error.InvalidOperation("Entry can be written only once in create mode.");
            }

            // In safe writing mode, we doesn't overwrite removed entries content.
            // In unsafe mode we pass underlying blocks to free segments, so them can be
            // overwritten.
            if ((!_safeWrite && !entry.IsNew) || entry.Written)
            {
                if (entry.EntryType == ArcFileEntryType.Store)
                {
                    _freeSegments.Add(new FileSegment(entry.Offset, entry.Length));
                }
                else if (entry.EntryType == ArcFileEntryType.Chunked)
                {
                    var lastChunkIndexExclusive = entry.ChunkIndex + entry.ChunkCount;
                    for (var chunkIndex = entry.ChunkIndex; chunkIndex < lastChunkIndexExclusive; chunkIndex++)
                    {
                        ref var chunk = ref _chunkTable[chunkIndex];
                        _freeSegments.Add(new FileSegment(chunk.Offset, chunk.CompressedLength));
                    }
                }
                else throw Error.Unreachable();
            }

            entry.EnterWriteLock();

            compressionLevel ??= _compressionLevel;

            _wasModified = true;
            _wasStoreBlockAllocated = false;

            // Init writing data
            _wHash = new Adler32().Hash; // TODO: Create struct-based adler32
            _wLength = 0;
            _wCompressedLength = 0;
            if (_wChunks.Allocated)
            {
                _wChunks.Clear();
            }
            else
            {
                _wChunks = new ReferenceValueList<ArcEntryChunk>(0);
            }

            ArcEntryStream entryStream;
            if (compressionLevel == CompressionLevel.NoCompression)
            {
                // TODO: in store mode delay write with final bit,
                // so if we done writing in single call - then we can allocate
                // free block from free blocks.

                _wEntryType = ArcFileEntryType.Store;
                _wFileOffset = checked((uint)AllocateStoreBlock(0));
                _wasStoreBlockAllocated = false;
                entryStream = new WritingStoreEntryStream(this, entryId, WritingStoreBufferSize);
            }
            else
            {
                _wEntryType = ArcFileEntryType.Chunked;
                _wFileOffset = 0;
                entryStream = new WritingChunkedEntryStream(this, entryId, _chunkLength,
                    compressionLevel.Value);
            }

            return _activeWritingStream = entryStream;
        }

        internal void Remove(arc_entry_id entryId)
        {
            ThrowIfDisposed();
            ThrowInReadMode();
            if (_mode == ArcArchiveMode.Create)
                throw Error.InvalidOperation("Removing records in create mode is not supported."); // TODO: rename message

            ref var entry = ref GetEntry(entryId);
            if (entry.IsRemoved)
            {
                throw Error.InvalidOperation("Attempt to remove already removed entry.");
            }
            if (entry.HasReadLock || entry.HasWriteLock)
            {
                throw Error.InvalidOperation("Attempt to remove entry which currently opened.");
            }

            if (_entryMap.Remove(entry.Name))
            {
                if (!_safeWrite)
                {
                    CalculateFreeSegmentsIfNeed();

                    if (entry.EntryType == ArcFileEntryType.Store)
                    {
                        _freeSegments.Add(new FileSegment(entry.Offset, entry.Length));
                    }
                    else if (entry.EntryType == ArcFileEntryType.Chunked)
                    {
                        ReadChunkTableIfNeed();

                        var lastChunkIndexExclusive = entry.ChunkIndex + entry.ChunkCount;
                        for (var chunkIndex = entry.ChunkIndex; chunkIndex < lastChunkIndexExclusive; chunkIndex++)
                        {
                            ref var chunk = ref _chunkTable[chunkIndex];
                            _freeSegments.Add(new FileSegment(chunk.Offset, chunk.CompressedLength));
                        }
                    }
                    else throw Error.Unreachable();
                }

                entry.CloseRemovedEntry();
                _wasModified = true;
            }
            else throw Error.Unreachable();
        }

        internal ArcEntryChunkCollection GetChunkInfo(arc_entry_id id)
        {
            ThrowIfDisposed();
            ReadChunkTableIfNeed();
            ref var entry = ref GetEntry(id);
            return GetChunkInfoInternal(ref entry);
        }

        private ArcEntryChunkCollection GetChunkInfoInternal(ref Entry entry)
        {
            if (entry.EntryType == ArcFileEntryType.Chunked)
            {
                var chunks = _chunkTable.AsMemory(entry.ChunkIndex, entry.ChunkCount);
                return new ArcEntryChunkCollection(chunks);
            }
            else
            {
                return new ArcEntryChunkCollection(default);
            }
        }

        #endregion

        #region Internal Reading Stream API

        internal void CloseReadingStream(ArcEntryStream entryStream)
        {
            ref var entry = ref GetEntry(entryStream.EntryId);
            _numberOfReadLocks--;
            entry.ExitReadLock();
        }

        internal int ReadBytes(Span<byte> output, uint offset, int length)
        {
            ThrowIfDisposed();

            var afStream = GetStream();
            afStream.Seek(offset);

            var bytesRead = afStream.Read(output.Slice(0, length));
            Check.True(bytesRead == length);
            return bytesRead;
        }

        internal int ReadChunk(in ArcEntryChunk chunk, Span<byte> output)
        {
            ThrowIfDisposed();

            Check.True(chunk.Length <= output.Length);

            if (chunk.MaybeStore && _format.SupportStoreChunks)
            {
                return ReadBytes(output, chunk.Offset, chunk.Length);
            }
            else
            {
                var afStream = GetStream();
                afStream.Seek(chunk.Offset);

                var decoder = GetDecoder();
                decoder.Decode(afStream.Stream, chunk.CompressedLength, output.Slice(0, chunk.Length));
                return chunk.Length;
            }
        }

        #endregion

        #region Internal Writing Stream API

        internal void CloseWritingStream(ArcEntryStream entryStream)
        {
            Check.True(_activeWritingStream == entryStream);

            ref var entry = ref GetEntry(entryStream.EntryId);

            if (_wEntryType == ArcFileEntryType.Store)
            {
                entry.CloseStoreEntry(
                    fileOffset: _wFileOffset,
                    compressedLength: _wCompressedLength,
                    length: _wLength,
                    hash: _wHash);
            }
            else if (_wEntryType == ArcFileEntryType.Chunked)
            {
                // Update chunks
                var firstChunkIndex = _chunkTable.Count;
                var chunkCount = _wChunks.Count;

                if (chunkCount > 0)
                {
                    for (var i = 0; i < chunkCount; i++)
                    {
                        var chunk = _wChunks[i]; // TODO: remove this temp copy
                        _chunkTable.Add(chunk);
                    }
                }

                entry.CloseChunkedEntry(
                    compressedLength: _wCompressedLength,
                    length: _wLength,
                    hash: _wHash,
                    chunkCount: chunkCount,
                    firstChunkIndex: firstChunkIndex
                    );

                _wChunks.Clear();
            }
            else throw Error.Unreachable();

            _activeWritingStream = null;
        }

        /// <summary>
        /// Write portion of bytes in entry mode.
        /// </summary>
        internal void WriteStoreBytes(ReadOnlySpan<byte> buffer, bool final)
        {
            DebugCheck.True(_activeWritingStream != null);

            if (final && !_wasStoreBlockAllocated)
            {
                // throw Error.NotImplemented("Allocate from chunk.");
            }

            var targetOffset = AllocateStoreBlock(buffer.Length);

            var afStream = GetStream();
            afStream.Seek(targetOffset);
            afStream.Write(buffer);

            _wHash = new Adler32(_wHash).ComputeHash(buffer);
            _wLength = checked((uint)(_wLength + buffer.Length));
            _wCompressedLength = checked((uint)(_wCompressedLength + buffer.Length));
        }

        internal void WriteChunkBytes(ReadOnlySpan<byte> buffer, CompressionLevel compressionLevel, bool final)
        {
            // Attempt to write chunk after downgrade will result in error.
            Check.True(_wEntryType == ArcFileEntryType.Chunked);

            var encoder = GetEncoder(compressionLevel);

            var encodedBuffer = encoder.EncodeToBuffer(buffer);
            try
            {
                var length = buffer.Length;
                var compressedLength = encodedBuffer.Length;

                ReadOnlySpan<byte> chunkData;

                var savedLength = length - compressedLength;
                var isProfitable = savedLength > 0
                    && savedLength > CompressedChunkThreshold;

                if (!isProfitable && final && _wChunks.Count == 0)
                {
                    // Downgrade to store
                    _wEntryType = ArcFileEntryType.Store;
                    chunkData = buffer;
                    compressedLength = length;
                }
                else if (!isProfitable && _format.SupportStoreChunks)
                {
                    // Uncompressed chunk.
                    chunkData = buffer;
                    compressedLength = length;
                }
                else
                {
                    // Compressed chunk.
                    chunkData = encodedBuffer.Span;
                }

                var chunkOffset = AllocateChunkBlock(chunkData.Length);

                var afStream = GetStream();
                afStream.Seek(chunkOffset);
                afStream.Write(chunkData);

                _wHash = new Adler32(_wHash).ComputeHash(buffer);
                _wLength = checked((uint)(_wLength + length));
                _wCompressedLength = checked((uint)(_wCompressedLength + compressedLength));

                if (_wEntryType == ArcFileEntryType.Store)
                {
                    _wFileOffset = checked((uint)chunkOffset);
                }
                else if (_wEntryType == ArcFileEntryType.Chunked)
                {
                    var chunk = new ArcEntryChunk(
                        offset: checked((uint)chunkOffset),
                        compressedLength: compressedLength,
                        length: length);

                    _wChunks.Add(chunk);
                }
            }
            finally
            {
                encodedBuffer.Return();
            }
        }

        #endregion

        #region Flush (For Writing)

        // TODO: (Low) (ArcArchive) Flush - does it need to be public?

        private void Flush()
        {
            if (_mode == ArcArchiveMode.Read) return;
            if (!_wasModified) return;

            ReadEntriesIfNeed();
            ReadChunkTableIfNeed();
            CalculateFreeSegmentsIfNeed();

            // TODO: (Medium) (ArcArchive) In any mode, we might attempt to calculate required space,
            // for TOC and allocate as free chunk (after any used chunks / data) (if them available).
            // The goal is to allow trim file as much as possible.

            var numberOfEntries = 0;
            var numberOfChunks = 0;

            var entryTableCount = _entryTable.Count;
            for (var i = 0; i < entryTableCount; i++)
            {
                ref var entry = ref _entryTable[i];

                if (entry.IsNew && !entry.Written)
                {
                    throw Error.InvalidOperation("Entry \"{0}\" was created but not written.", entry.Name);
                }

                if (!entry.IsRemoved)
                {
                    Check.True(entry.Name != null);
                    Check.True(entry.ChunkCount >= 0);

                    numberOfEntries++;
                    numberOfChunks += entry.ChunkCount;
                }
            }

            _header.EntryCount = numberOfEntries;
            _header.ChunkCount = numberOfChunks;
            _header.ChunkTableLength = checked((uint)(numberOfChunks * ArcEntryChunk.Size));
            _header.ChunkTableOffset = checked((uint)_freeOffset);

            // Chunk Table
            var afStream = GetStream();
            afStream.Seek(_header.ChunkTableOffset);
            {
                if (_chunkTable.Count != _header.ChunkCount)
                {
                    // Rebuild chunk table, if needed.
                    var newChunkTable = new ReferenceValueList<ArcEntryChunk>(_chunkTable.Count);
                    for (var i = 0; i < _entryTable.Count; i++)
                    {
                        ref var entry = ref _entryTable[i];
                        if (!entry.IsRemoved)
                        {
                            var newChunkIndex = newChunkTable.Count;
                            var chunkIndex = entry.ChunkIndex;
                            var endAt = chunkIndex + entry.ChunkCount;
                            for (var ci = entry.ChunkIndex; ci < endAt; ci++)
                            {
                                newChunkTable.Add(in _chunkTable[ci]);
                            }
                            entry.SetChunkIndex(newChunkIndex);
                        }
                    }
                    _chunkTable = newChunkTable;
                }
                for (var i = 0; i < _chunkTable.Count; i++)
                {
                    afStream.WriteChunk(in _chunkTable[i]);
                }
            }

            // String Table
            {
                uint stringOffset = 0;
                for (var i = 0; i < entryTableCount; i++)
                {
                    ref var entry = ref _entryTable[i];

                    int bytesWritten;
                    if (!entry.IsRemoved)
                    {
                        afStream.WriteString(entry.Name, out var length, out bytesWritten);
                        entry.SetEncodedString(stringOffset, length);
                    }
                    else
                    {
                        bytesWritten = 0;
                        entry.SetEncodedString(0, 0);
                    }
                    stringOffset = checked((uint)(stringOffset + bytesWritten));
                }
                _header.StringTableLength = stringOffset;
            }

            // Entry Table
            {
                for (var i = 0; i < entryTableCount; i++)
                {
                    ref var entry = ref _entryTable[i];
                    if (!entry.IsRemoved)
                    {
                        entry.GetFileEntry(out var afEntry);
                        afStream.WriteEntry(in afEntry);
                    }
                }
            }

            // Trim End
            _stream.SetLength(_stream.Position);

            WriteHeader();
        }

        private void WriteHeader()
        {
            DebugCheck.True(_mode != ArcArchiveMode.Read);

            var afStream = GetStream();

            // Header
            afStream.Seek(0);
            afStream.WriteHeader(in _header);

            if (_mode == ArcArchiveMode.Create)
            {
                var headerAreaPadding = _headerAreaLength - ArcFileHeader.Size;
                if (headerAreaPadding > 0)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(headerAreaPadding);
                    try
                    {
                        var bufferSpan = new Span<byte>(buffer, 0, headerAreaPadding);
                        bufferSpan.Fill(0);
                        afStream.Write(bufferSpan);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }
        }

        #endregion

        #region Block Allocation (For Writing)

        private long AllocateStoreBlock(int length)
        {
            Check.That(_freeOffset > 0);

            _wasStoreBlockAllocated = true;

            var offset = _freeOffset;
            _freeOffset += length;
            return offset;
        }

        private long AllocateChunkBlock(int length)
        {
            CalculateFreeSegmentsIfNeed();

            for (var i = 0; i < _freeSegments.Count; i++)
            {
                ref var freeSegment = ref _freeSegments[i];
                if (freeSegment.Length >= length)
                {
                    var resultOffset = freeSegment.Offset;
                    freeSegment.Offset += length;
                    freeSegment.Length -= length;

                    DebugCheck.That(freeSegment.Length >= 0);

                    if (freeSegment.Length == 0)
                    {
                        // TODO: (Low) (ArcArchive) Remove zero-length free segment.
                    }

                    return resultOffset;
                }
            }

            return AllocateStoreBlock(length);
        }

        private void CalculateFreeSegmentsIfNeed()
        {
            if (_hasFreeSegments) return;
            CalculateFreeSegmentsCore();
        }

        private void CalculateFreeSegmentsCore()
        {
            ReadEntriesIfNeed();
            ReadChunkTableIfNeed();

            long firstUsedOffset = 0;

            var allocatedBlocks = new List<FileSegment>(_entryTable.Count + _chunkTable.Count);
            for (var i = 0; i < _entryTable.Count; i++)
            {
                ref var entry = ref _entryTable[i];
                Check.True(!entry.HasWriteLock);
                if (!entry.IsRemoved)
                {
                    if (entry.EntryType == ArcFileEntryType.Store)
                    {
                        allocatedBlocks.Add(new FileSegment(entry.Offset, entry.Length));
                    }
                    else if (entry.EntryType == ArcFileEntryType.Chunked)
                    {
                        var lastChunkIndexExclusive = entry.ChunkIndex + entry.ChunkCount;
                        for (var chunkIndex = entry.ChunkIndex; chunkIndex < lastChunkIndexExclusive; chunkIndex++)
                        {
                            ref var chunk = ref _chunkTable[chunkIndex];
                            allocatedBlocks.Add(new FileSegment(chunk.Offset, chunk.CompressedLength));
                        }
                    }
                    else throw Error.Unreachable();
                }
            }

            var freeSegments = new ReferenceValueList<FileSegment>(0);
            if (allocatedBlocks.Count > 0)
            {
                allocatedBlocks = allocatedBlocks.OrderBy(x => x.Offset).ToList();

                firstUsedOffset = allocatedBlocks[0].Offset;
                if (firstUsedOffset > _headerAreaLength)
                {
                    freeSegments.Add(new FileSegment(_headerAreaLength, firstUsedOffset - _headerAreaLength));
                }

                for (var i = 1; i < allocatedBlocks.Count; i++)
                {
                    var previousBlock = allocatedBlocks[i - 1];
                    var thisBlock = allocatedBlocks[i];

                    var freeOffset = previousBlock.Offset + previousBlock.Length;
                    var freeLength = thisBlock.Offset - freeOffset;
                    if (freeLength > 0)
                    {
                        freeSegments.Add(new FileSegment(freeOffset, freeLength));
                    }
                }

                // Ending
                DebugCheck.That(_header.ChunkTableOffset > 0);
                var lastAllocatedBlock = allocatedBlocks[allocatedBlocks.Count - 1];
                var lastFreeOffset = lastAllocatedBlock.Offset + lastAllocatedBlock.Length;
                var lastFreeLength = _header.ChunkTableOffset - lastFreeOffset;
                if (lastFreeLength > 0)
                {
                    // TODO: (Medium) (ArcArchive) Always report free segment, otherwise it doesn't indicate correctly.
                    // Also in flush probably we may write into it, if it no linear segments written.

                    if (_safeWrite)
                    {
                        freeSegments.Add(new FileSegment(lastFreeOffset, lastFreeLength));

                        _freeOffset = _header.ChunkTableOffset
                            + _header.ChunkTableLength
                            + _header.StringTableLength
                            + _header.EntryCount * ArcFileEntry.Size;

                        Check.That(_freeOffset >= (lastFreeOffset + lastFreeLength));
                    }
                    else
                    {
                        _freeOffset = lastFreeOffset;
                    }
                }
                else
                {
                    if (_safeWrite)
                    {
                        _freeOffset = _header.ChunkTableOffset
                            + _header.ChunkTableLength
                            + _header.StringTableLength
                            + _header.EntryCount * ArcFileEntry.Size;

                        Check.That(_freeOffset >= lastFreeOffset);
                    }
                    else
                    {
                        _freeOffset = lastFreeOffset;
                    }
                }
            }
            else
            {
                // Empty archive.
                _freeOffset = _headerAreaLength;
            }

            Check.That(_freeOffset > 0);

            _freeSegments = freeSegments;
            _hasFreeSegments = true;
        }

        #endregion

        #region Decoder

        private Decoder GetDecoder()
        {
            if (_decoder != null) return _decoder;
            return _decoder = CreateDecoder();
        }

        private Decoder CreateDecoder()
        {
            if (_format.ZlibCompression)
            {
                if ((_useLibDeflate ?? ZlibLibDeflateDecoder.IsSupported) == true)
                {
                    return new ZlibLibDeflateDecoder();
                }
                else return ZlibDecoder.Shared;
            }
            else if (_format.Lz4Compression)
            {
                return Lz4Decoder.Shared;
            }
            else throw Error.InvalidOperation("Unknown compression algorithm.");
        }

        #endregion

        #region Encoder

        private Encoder GetEncoder(CompressionLevel compressionLevel)
        {
            // TODO: Make CompressionLevel as encoder's property?
            DebugCheck.True(compressionLevel != CompressionLevel.NoCompression);

            if (_encoder != null && _encoderCompressionLevel == compressionLevel)
            {
                return _encoder;
            }
            else
            {
                _encoder?.Dispose();
                _encoderCompressionLevel = compressionLevel;
                return _encoder = CreateEncoder(compressionLevel);
            }
        }

        private Encoder CreateEncoder(CompressionLevel compressionLevel)
        {
            if (_format.ZlibCompression)
            {
                if ((_useLibDeflate ?? ZlibLibDeflateEncoder.IsSupported) == true)
                {
                    return new ZlibLibDeflateEncoder(compressionLevel);
                }
                else return new ZlibEncoder(compressionLevel);
            }
            else if (_format.Lz4Compression)
            {
                return new Lz4Encoder(compressionLevel);
            }
            else throw Error.InvalidOperation("Unknown compression algorithm.");
        }

        #endregion

        #region Reading - Header

        private void ReadAndCheckHeader()
        {
            Check.True(_mode == ArcArchiveMode.Read || _mode == ArcArchiveMode.Update);

            var afStream = GetStream();
            afStream.Seek(0);
            afStream.ReadHeader(out var afHeader);

            if (afHeader.Magic != ArcHeaderMagic)
            {
                throw Error.InvalidOperation("Not a ARC file.");
            }

            // TODO: (Medium) (ArcArchive) Enforce format when reading/updating.
            if (afHeader.Version == 1)
            {
                // Titan Quest
                // Titan Quest: Immortal Throne
                // Titan Quest: Anniversary Edition
                _format = ArcFileFormat.FromVersion(1);
            }
            else if (afHeader.Version == 3)
            {
                // Grim Dawn
                _format = ArcFileFormat.FromVersion(3);
            }
            else throw Error.InvalidOperation("Unkown ARC file version.");

            if (afHeader.ChunkTableLength != (afHeader.ChunkCount * ArcEntryChunk.Size))
            {
                throw Error.InvalidOperation("ChunkTableSize doesn't match to NumberOfChunkEntries.");
            }

            // TODO: (ArcArchive) Validate Header

            _header = afHeader;
        }

        #endregion

        #region Reading - Entries

        private void ReadEntriesIfNeed()
        {
            if (_hasEntryTable) return;
            ReadEntriesCore();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReadEntriesCore()
        {
            DebugCheck.True(!_hasEntryTable);

            var afStream = GetStream();

            var entryTable = new ReferenceValueList<Entry>(checked((int)_header.EntryCount));

            afStream.Seek(_header.GetTocOffset());
            for (var i = 0; i < _header.EntryCount; i++)
            {
                afStream.ReadEntry(out var afEntry);
                entryTable.Add(new Entry(in afEntry));
            }

            // Read String table and create entries
            var entryMap = new EntryMap(GetEntryMapStringComparer());
            for (var i = 0; i < entryTable.Count; i++)
            {
                ref var entry = ref entryTable[i];
                if (!entry.IsRemoved)
                {
                    var stringOffset = _header.GetStringTableOffset() + entry.NameStringOffset;
                    var stringLength = entry.NameStringLength;
                    // TODO: validate string location

                    afStream.Seek(stringOffset);
                    var entryName = afStream.ReadString(checked((int)stringLength));

                    entry.Name = entryName;

                    entryMap.Add(entryName, (arc_entry_id)i);
                }
            }

            _entryTable = entryTable;
            _entryMap = entryMap;
            _hasEntryTable = true;
        }

        #endregion

        #region Reading - ChunkTable

        private void ReadChunkTableIfNeed()
        {
            if (_hasChunkTable) return;
            ReadChunkTableCore();
        }

        private void ReadChunkTableCore()
        {
            var chunkCount = _header.ChunkCount;
            var chunkTable = new ReferenceValueList<ArcEntryChunk>(chunkCount);

            if (chunkCount > 0)
            {
                var afStream = GetStream();
                afStream.Seek(_header.ChunkTableOffset);
                for (var i = 0; i < chunkCount; i++)
                {
                    afStream.ReadChunk(out var chunk);
                    // TODO: Validate chunk
                    chunkTable.Add(chunk);
                }
            }

            _chunkTable = chunkTable;
            _hasChunkTable = true;
        }

        #endregion

        #region EntryMap

        private EntryMap GetEntryMap()
        {
            if (_entryMap != null) return _entryMap;
            throw Error.Unreachable();
        }

        #endregion

        #region Create

        private void CreateHeaderAndCatalog()
        {
            _header = new ArcFileHeader();
            _header.Magic = ArcHeaderMagic;
            _header.Version = _format.Version;
            _header.ChunkTableOffset = (uint)_headerAreaLength;

            _wasModified = true;
            _freeOffset = 0;

            _entryTable = new ReferenceValueList<Entry>(0);
            _chunkTable = new ReferenceValueList<ArcEntryChunk>(0);
            _entryMap = new EntryMap(GetEntryMapStringComparer());
            _hasEntryTable = true;
            _hasChunkTable = true;
        }

        #endregion

        #region Defragment

        private void DefragmentCore(IIncrementalProgress<long>? progress)
        {
            var fragmentedEntries = GetFragmentedEntries(out var fragmentedChunkCount);
            if (fragmentedEntries.Count > 0)
            {
                DebugCheck.That(_hasEntryTable);
                DebugCheck.That(_hasChunkTable);

                progress?.AddMaximumValue(fragmentedEntries.Count);

                foreach (var entryId in fragmentedEntries)
                {
                    ref var entry = ref _entryTable[(int)entryId];

                    Check.That(!entry.IsRemoved);
                    Check.That(entry.EntryType == ArcFileEntryType.Chunked);

                    var lastChunkIndexExclusive = entry.ChunkIndex + entry.ChunkCount;
                    for (var chunkIndex = entry.ChunkIndex; chunkIndex < lastChunkIndexExclusive; chunkIndex++)
                    {
                        ref var chunk = ref _chunkTable[chunkIndex];

                        var outputOffset = AllocateStoreBlock(chunk.CompressedLength);
                        CopySegment(chunk.Offset, chunk.CompressedLength, outputOffset);
                        chunk.Offset = checked((uint)outputOffset);

                        _wasModified = true;
                    }

                    progress?.AddValue(1);
                }
            }
        }

        private List<arc_entry_id> GetFragmentedEntries(out int fragmentedChunkCount)
        {
            ReadEntriesIfNeed();
            ReadChunkTableIfNeed();

            var result = new List<arc_entry_id>();
            fragmentedChunkCount = 0;

            for (var i = 0; i < _entryTable.Count; i++)
            {
                ref var entry = ref _entryTable[i];

                if (!entry.IsRemoved)
                {
                    if (entry.EntryType == ArcFileEntryType.Chunked)
                    {
                        var previousChunk = default(ArcEntryChunk);
                        var lastChunkIndexExclusive = entry.ChunkIndex + entry.ChunkCount;
                        for (var chunkIndex = entry.ChunkIndex; chunkIndex < lastChunkIndexExclusive; chunkIndex++)
                        {
                            ref var chunk = ref _chunkTable[chunkIndex];

                            if (chunkIndex > entry.ChunkIndex)
                            {
                                if (chunk.Offset != (previousChunk.Offset + previousChunk.CompressedLength))
                                {
                                    result.Add((arc_entry_id)i);
                                    fragmentedChunkCount += entry.ChunkCount;
                                    break;
                                }
                            }
                            previousChunk = chunk;
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Compact

        private void CompactCore(bool repack, CompressionLevel compressionLevel, IIncrementalProgress<long>? progress)
        {
            DebugCheck.That(_numberOfReadLocks == 0);
            DebugCheck.That(_activeWritingStream == null);

            // Get allocated segments
            var allocatedSegments = GetAllocatedSegments();
            allocatedSegments = allocatedSegments.OrderBy(x => x.Offset).ToList();

            long minAllocatedOffset = 0;
            if (allocatedSegments.Count > 0)
            {
                minAllocatedOffset = allocatedSegments[0].Offset;
            }

            progress?.AddMaximumValue(allocatedSegments.Count);

            long destinationOffset = Math.Min(minAllocatedOffset, _headerAreaLength);

            for (var i = 0; i < allocatedSegments.Count; i++)
            {
                var segment = allocatedSegments[i];
                ref var entry = ref _entryTable[(int)segment.EntryId];

                if (repack && TryRepackSegment(ref segment, compressionLevel, out var buffer))
                {
                    Check.That(segment.IsChunk);

                    var afStream = GetStream();
                    afStream.Seek(destinationOffset);
                    afStream.Write(buffer.Span);

                    ref var chunk = ref _chunkTable[segment.ChunkIndex];
                    var compressedLengthDiff = buffer.Length - chunk.CompressedLength;

                    chunk.Offset = checked((uint)destinationOffset);
                    chunk.CompressedLength = buffer.Length;
                    entry.AddCompressedLength(compressedLengthDiff);

                    _wasModified = true;
                    destinationOffset += buffer.Length;

                    buffer.Return();
                }
                else
                {
                    Check.That(destinationOffset <= segment.Offset);

                    if (destinationOffset != segment.Offset)
                    {
                        MoveSegment(segment.Offset, segment.Length, destinationOffset);
                        if (segment.IsChunk)
                        {
                            ref var chunk = ref _chunkTable[segment.ChunkIndex];
                            chunk.Offset = checked((uint)destinationOffset);
                        }
                        else
                        {
                            entry.SetOffset(checked((uint)destinationOffset));
                        }

                        _wasModified = true;
                    }

                    destinationOffset += segment.Length;
                }

                progress?.AddValue(1);
            }

            _hasFreeSegments = false;
            _freeSegments.Clear();

            _freeOffset = destinationOffset;

            // Flush archive if TOC should be relocated.
            _wasModified |= _freeOffset != _header.ChunkTableOffset;
        }

        private bool TryRepackSegment(ref AllocatedSegment segment, CompressionLevel compressionLevel, out DataBuffer outputBuffer)
        {
            if (segment.IsChunk)
            {
                ref var entry = ref _entryTable[(int)segment.EntryId];

                Check.That(entry.EntryType == ArcFileEntryType.Chunked);
                ref var chunk = ref _chunkTable[segment.ChunkIndex];

                var chunkDataBuffer = ArrayPool<byte>.Shared.Rent(chunk.Length);
                try
                {
                    var chunkSpan = new Span<byte>(chunkDataBuffer, 0, chunk.Length);

                    var afStream = GetStream();
                    afStream.Seek(chunk.Offset);

                    if (chunk.MaybeStore && _format.SupportStoreChunks)
                    {
                        var bytesRead = afStream.Read(chunkSpan);
                        Check.True(bytesRead == chunkSpan.Length);
                    }
                    else
                    {
                        GetDecoder().Decode(afStream.Stream, chunk.CompressedLength, chunkSpan);
                    }

                    var cb = GetEncoder(compressionLevel).EncodeToBuffer(chunkSpan);
                    if (cb.Length < chunk.CompressedLength)
                    {
                        // New chunk is smaller, so return it.
                        outputBuffer = cb;
                        return true;
                    }
                    else
                    {
                        cb.Return();
                        outputBuffer = default;
                        return false;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(chunkDataBuffer);
                }
            }
            else
            {
                outputBuffer = default;
                return false;
            }
        }

        private void MoveSegment(long inputOffset, long inputLength, long outputOffset)
        {
            CopySegment(inputOffset, inputLength, outputOffset);
        }

        private void CopySegment(long inputOffset, long inputLength, long outputOffset)
        {
            Check.That(inputOffset != outputOffset);

            // Check what segments doesn't overlapped.
            if (outputOffset > inputOffset)
            {
                Check.That((inputOffset + inputLength) <= outputOffset);
            }

            var afStream = GetStream();

            var buffer = ArrayPool<byte>.Shared.Rent(256 * 1024);

            while (inputLength > 0)
            {
                var bytesToRead = (int)Math.Min(buffer.Length, inputLength);
                afStream.Seek(inputOffset);
                var bytesRead = afStream.Read(new Span<byte>(buffer, 0, bytesToRead));
                Check.That(bytesRead == bytesToRead);

                afStream.Seek(outputOffset);
                afStream.Write(new ReadOnlySpan<byte>(buffer, 0, bytesToRead));

                inputLength -= bytesToRead;
                inputOffset += bytesToRead;
                outputOffset += bytesToRead;
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }

        private List<AllocatedSegment> GetAllocatedSegments()
        {
            var allocatedSegments = new List<AllocatedSegment>(_entryTable.Count + _chunkTable.Count);
            for (var i = 0; i < _entryTable.Count; i++)
            {
                ref var entry = ref _entryTable[i];
                Check.True(!entry.HasReadLock && !entry.HasWriteLock);
                if (!entry.IsRemoved)
                {
                    if (entry.EntryType == ArcFileEntryType.Store)
                    {
                        allocatedSegments.Add(new AllocatedSegment(entry.Offset, entry.Length, (arc_entry_id)i));
                    }
                    else if (entry.EntryType == ArcFileEntryType.Chunked)
                    {
                        var lastChunkIndexExclusive = entry.ChunkIndex + entry.ChunkCount;
                        for (var chunkIndex = entry.ChunkIndex; chunkIndex < lastChunkIndexExclusive; chunkIndex++)
                        {
                            ref var chunk = ref _chunkTable[chunkIndex];
                            allocatedSegments.Add(new AllocatedSegment(chunk.Offset, chunk.CompressedLength,
                                (arc_entry_id)i, chunkIndex));
                        }
                    }
                    else throw Error.Unreachable();
                }
            }
            return allocatedSegments;
        }

        #endregion

        private ArcStream GetStream()
        {
            return new ArcStream(_stream);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw Error.ObjectDisposed(GetType().ToString());
        }

        private void ThrowInReadMode()
        {
            if (_mode == ArcArchiveMode.Read)
                throw Error.InvalidOperation("Can't open stream for writing when archive in read only mode.");
        }

        private void ThrowInCreateMode()
        {
            if (_mode == ArcArchiveMode.Create)
                throw Error.InvalidOperation("Open stream for reading is not allowed in create mode.");
        }

        private static StringComparer GetEntryMapStringComparer()
            => StringComparer.Ordinal;

        private static void ValidateOptions(ArcArchiveOptions options)
        {
            if (options.Mode == ArcArchiveMode.Create)
            {
                if (!options.Format.Complete)
                {
                    throw Error.Argument(nameof(options),
                        "You must specify layout when creating new archive.");
                }
            }
        }

        private static void ValidateStreamAndMode(IO.Stream stream, ArcArchiveMode mode)
        {
            switch (mode)
            {
                case ArcArchiveMode.Read:
                    if (!stream.CanRead || !stream.CanSeek)
                    {
                        throw Error.Argument(nameof(stream),
                            "Read mode requires a stream with read and seek capabilities.");
                    }
                    break;

                case ArcArchiveMode.Update:
                    if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
                    {
                        throw Error.Argument(nameof(stream),
                            "Update mode requires a stream with read, write, and seek capabilities.");
                    }
                    break;

                case ArcArchiveMode.Create:
                    if (!stream.CanWrite || !stream.CanSeek)
                    {
                        throw Error.Argument(nameof(stream),
                            "Create mode requires a stream with write and seek capability.");
                    }
                    break;

                default:
                    throw Error.Argument(nameof(mode));
            }
        }

        private static IO.FileStream OpenStream(string path, ArcArchiveMode mode)
        {
            switch (mode)
            {
                case ArcArchiveMode.Read:
                    return IO.File.Open(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read);

                case ArcArchiveMode.Update:
                    return IO.File.Open(path, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.None);

                case ArcArchiveMode.Create:
                    return IO.File.Open(path, IO.FileMode.Create, IO.FileAccess.ReadWrite, IO.FileShare.None);

                default: throw Error.Unreachable();
            }
        }
    }
}
