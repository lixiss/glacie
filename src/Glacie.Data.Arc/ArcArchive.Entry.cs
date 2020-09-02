using System;

namespace Glacie.Data.Arc
{
    partial class ArcArchive
    {
        internal struct Entry
        {
            private string _name;
            private EntryFlags _flags;
            private ArcFileEntry _entry;

            public Entry(string name)
            {
                _name = name;
                _flags = EntryFlags.IsNew;
                _entry = default;
                _entry.EntryType = ArcFileEntryType.Removed;
            }

            public Entry(in ArcFileEntry afEntry)
            {
                _name = null!;
                _flags = EntryFlags.None;
                _entry = afEntry;
            }

            public string Name
            {
                get => _name;
                internal set => _name = value;
            }

            public ArcFileEntryType EntryType
            {
                get
                {
                    ThrowIfInvalidState();
                    return _entry.EntryType;
                }
            }

            public uint Offset => _entry.Offset;

            public uint CompressedLength
            {
                get
                {
                    ThrowIfInvalidState();
                    return _entry.CompressedLength;
                }
            }

            public uint Length
            {
                get
                {
                    ThrowIfInvalidState();
                    return _entry.Length;
                }
            }

            public uint Hash
            {
                get
                {
                    ThrowIfInvalidState();
                    return _entry.Hash;
                }
            }

            public long Timestamp
            {
                get
                {
                    ThrowIfInvalidState();
                    return _entry.Timestamp;
                }
            }

            public int ChunkCount => _entry.ChunkCount;

            public int ChunkIndex => _entry.ChunkIndex;

            public uint NameStringOffset => _entry.NameStringOffset;

            public int NameStringLength => _entry.NameStringLength;

            internal void GetFileEntry(out ArcFileEntry afEntry)
            {
                afEntry = _entry;
            }

            public bool IsRemoved
            {
                get => _entry.EntryType == ArcFileEntryType.Removed
                    || ((_flags & EntryFlags.Removed) != 0);
            }

            public bool IsNew
            {
                get => (_flags & EntryFlags.IsNew) != 0;
            }

            public bool Written
            {
                get => (_flags & EntryFlags.Written) != 0;
            }

            public bool HasReadLock
            {
                get => (_flags & EntryFlags.ReadLockMask) != 0;
            }

            public bool HasWriteLock
            {
                get => (_flags & EntryFlags.WriteLock) != 0;
            }

            public void EnterReadLock()
            {
                _flags = checked((EntryFlags)((int)_flags + (int)EntryFlags.ReadLockIncrement));
            }

            public void ExitReadLock()
            {
                Check.That(HasReadLock);
                _flags = checked((EntryFlags)((int)_flags - (int)EntryFlags.ReadLockIncrement));
            }

            public void EnterWriteLock()
            {
                _flags = (_flags & ~EntryFlags.WriteLock) | EntryFlags.WriteLock;
            }

            public void ExitWriteLock()
            {
                Check.That(HasWriteLock);
                _flags &= ~EntryFlags.WriteLock;
            }

            public void CloseRemovedEntry()
            {
                DebugCheck.True(_entry.EntryType != ArcFileEntryType.Removed);

                // Setup only flag, so entry's data still occupy their blocks.
                _flags |= EntryFlags.Removed;

                //_entry.EntryType = ArcFileEntryType.Removed;
                //_fileOffset = 0;
                //_compressedSize = 0;
                //_decompressedSize = 0;
                //_decompressedHash = 0;
                //_timestamp = 0;
                //_chunkCount = 0;
                //_firstChunkIndex = 0;
                //_stringEntryOffset = 0;
                //_stringEntryLength = 0;
            }

            public void CloseStoreEntry(uint fileOffset, uint compressedLength, uint length, uint hash)
            {
                DebugCheck.True(HasWriteLock);

                _entry.EntryType = ArcFileEntryType.Store;
                _entry.Offset = fileOffset;
                _entry.CompressedLength = compressedLength;
                _entry.Length = length;
                _entry.Hash = hash;
                _entry.Timestamp = DateTimeOffset.UtcNow.ToFileTime();
                _entry.ChunkCount = 0;
                _entry.ChunkIndex = 0;
                _entry.NameStringOffset = 0;
                _entry.NameStringLength = 0;

                ExitWriteLock();
                _flags |= EntryFlags.Written;
            }

            public void CloseChunkedEntry(uint compressedLength, uint length, uint hash, int chunkCount, int firstChunkIndex)
            {
                DebugCheck.True(HasWriteLock);

                _entry.EntryType = ArcFileEntryType.Chunked;
                _entry.Offset = 0;
                _entry.CompressedLength = compressedLength;
                _entry.Length = length;
                _entry.Hash = hash;
                _entry.Timestamp = DateTimeOffset.UtcNow.ToFileTime();
                _entry.ChunkCount = chunkCount;
                _entry.ChunkIndex = firstChunkIndex;
                _entry.NameStringOffset = 0;
                _entry.NameStringLength = 0;

                ExitWriteLock();
                _flags |= EntryFlags.Written;
            }

            public void SetEncodedString(uint offset, int length)
            {
                _entry.NameStringLength = length;
                _entry.NameStringOffset = offset;
            }

            public void SetOffset(uint value)
            {
                _entry.Offset = value;
            }

            public void SetChunkIndex(int chunkIndex)
            {
                _entry.ChunkIndex = chunkIndex;
            }

            public void SetTimestamp(long value)
            {
                _entry.Timestamp = value;
            }

            public void AddCompressedLength(int value)
            {
                long compressedLength = _entry.CompressedLength;
                compressedLength += value;
                _entry.CompressedLength = checked((uint)compressedLength);
            }

            private void ThrowIfInvalidState()
            {
                if (_entry.EntryType == ArcFileEntryType.Removed
                    || ((_flags & EntryFlags.Removed) != 0))
                    throw Error.InvalidOperation("This property is not available because the entry was removed.");

                if ((_flags & ~(EntryFlags.IsNew | EntryFlags.Written)) == EntryFlags.IsNew)
                    throw Error.InvalidOperation("This property is not available because the entry has been written to or modified.");

                if ((_flags & ~(EntryFlags.Written | EntryFlags.WriteLock)) == EntryFlags.WriteLock)
                    throw Error.InvalidOperation("This property is not available because the entry is currently being written.");
            }

            [Flags]
            private enum EntryFlags : int
            {
                None = 0,

                IsNew = 1 << 0,
                WriteLock = 1 << 1,
                Written = 1 << 2,
                Removed = 1 << 3,

                ReadLockMask = 0x7FFFFFF0,
                ReadLockIncrement = 1 << 4,
            }
        }
    }
}
