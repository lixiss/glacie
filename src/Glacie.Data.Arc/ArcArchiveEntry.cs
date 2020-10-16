using System;
using System.Runtime.CompilerServices;
using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;
using Glacie.Utilities;

using IO = System.IO;

namespace Glacie.Data.Arc
{
    public readonly struct ArcArchiveEntry
    {
        private readonly ArcArchive _archive;
        private readonly arc_entry_id _id;

        internal ArcArchiveEntry(ArcArchive archive, arc_entry_id id)
        {
            _archive = archive;
            _id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref ArcArchive.Entry GetEntry() => ref _archive.GetEntry(_id);

        public string Name => GetEntry().Name;

        public uint Length => GetEntry().Length;

        public uint CompressedLength => GetEntry().CompressedLength;

        /// <summary>
        /// Adler32 checksum calculated over uncompressed data.
        /// </summary>
        public uint Hash => GetEntry().Hash;

        // TODO: (Low) (ArcEntry) Consider to keep Timestamp if it was changed after close of writer stream.
        // and put real time if it was not changed.
        public long Timestamp
        {
            get => GetEntry().Timestamp;
            set => _archive.SetEntryTimestamp(_id, value);
        }

        /// <summary>
        /// Gets or sets the last time the entry in the arc archive was changed.
        /// </summary>
        /// <remarks>
        /// This property calculated from <see cref="Timestamp"/>.
        /// This property may throw exception if timestamp value can't be
        /// represented as DateTimeOffset.
        /// </remarks>
        public DateTimeOffset LastWriteTime
        {
            get => TimestampUtilities.ToDateTimeOffest(Timestamp);
            set => Timestamp = TimestampUtilities.FromDateTimeOffset(value);
        }

        public bool TryGetLastWriteTime(out DateTimeOffset result)
        {
            return TimestampUtilities.TryConvert(Timestamp, out result);
        }

        public IO.Stream Open()
        {
            return _archive.OpenReadingStream(_id);
        }

        public IO.Stream OpenWrite(CompressionLevel? compressionLevel = null)
        {
            return _archive.OpenWritingStream(_id, compressionLevel);
        }

        /// <summary>
        /// Removes the entry from the archive.
        /// </summary>
        public void Remove()
        {
            _archive.Remove(_id);
        }

        // TODO: (Low) (ArcEntry) EntryType should has public enumeration.
        public int EntryType => (int)GetEntry().EntryType;

        /// <summary>
        /// Returns information about entry chunks.
        /// This is infrastructure method, and doesn't intended for direct use.
        /// </summary>
        public ArcEntryChunkCollection GetChunkInfo()
        {
            return _archive.GetChunkInfo(_id);
        }
    }
}
