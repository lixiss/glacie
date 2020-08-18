﻿using System;
using System.Collections.Generic;
using System.Linq;
using Glacie.Buffers;
using Glacie.Data.Arz.FileFormat;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Data.Arz.Utilities;
using Glacie.Data.Compression;
using Glacie.Data.Compression.Utilities;
using IO = System.IO;

namespace Glacie.Data.Arz
{
    // TODO: (VeryLow) (Decision) Split ArzFileContext into ArzFileContext and ArzReaderContext
    // First should just be bounded to a file, while second do actual job.
    // TODO: (VeryLow) (ArzFileContext) (Decision) ArzFileContext -> ArzReaderContext ?

    /// <remarks>
    /// Keeps reference to FileStream to keep file opened (and locked for changes)
    /// for whole life time of ArzDatabase.
    /// </remarks>
    internal sealed partial class ArzFileContext : ArzContext
    {
        #region Factory

        public static ArzDatabase Open(string path, ArzReaderOptions? options)
        {
            ArzFileContext? context = null;
            try
            {
                // Context may keep file openeded until it disposed.
                context = new ArzFileContext(path, OpenStream(path), options ?? new ArzReaderOptions());

                context.Initialize();
                var database = context.Database;

                // Database itself takes ownership for ArzContext, so we 
                // should not dispose it. However if ReadCore throw exception,
                // we will dispose context.
                context = null;

                return database;
            }
            finally
            {
                context?.Dispose();
            }
        }

        public static ArzDatabase Open(IO.Stream stream, ArzReaderOptions? options)
        {
            ArzFileContext? context = null;
            try
            {
                // Context may keep stream openeded until it disposed.
                context = new ArzFileContext(null, stream, options ?? new ArzReaderOptions());

                context.Initialize();
                var database = context.Database;

                // Database itself takes ownership for ArzContext, so we 
                // should not dispose it. However if ReadCore throw exception,
                // we will dispose context.
                context = null;

                return database;
            }
            finally
            {
                context?.Dispose();
            }
        }

        #endregion

        private readonly IO.Stream _stream;

        private Decoder? _decoder;

        // Options
        private readonly ArzReadingMode _fieldDataLoadingMode = ArzReadingMode.Lazy;
        private readonly bool _multithreadedReading = true;
        private readonly int _maxDegreeOfParallelism = -1;
        private readonly bool _useLibDeflate = false;
        private ArzFileLayout _afLayout;

        #region Construction & Destruction

        private ArzFileContext(string? path, IO.Stream stream, ArzReaderOptions options)
            : base(path)
        {
            _stream = stream;

            _fieldDataLoadingMode = options.Mode;
            _multithreadedReading = options.Multithreaded;
            _maxDegreeOfParallelism = options.MaxDegreeOfParallelism;
            _useLibDeflate = options.UseLibDeflate ?? ZlibLibDeflateDecoder.IsSupported;
            _afLayout = options.Layout;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream?.Dispose();
                _decoder?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        public override bool HasLayoutInference => true;

        public override ArzFileLayout Layout => _afLayout;

        protected override InitializeResult InitializeCore()
        {
            var afStream = new ArzFileStream(GetStream());

            // 1. Header
            ArzFileHeader afHeader;
            {
                afStream.Seek(0);
                afStream.ReadHeader(out afHeader);
            }

            // TODO: (Medium) (ArzFileContext) Do some cheap validations here:
            // - from header (offsets+size should be located inside file)
            // - known / unkown magic/versions (should be part of file layout)

            if (_afLayout != ArzFileLayout.None)
            {
                if (!_afLayout.IsValid()) throw ArzError.InvalidLayout();

                // TODO: (Medium) (ArzFileContext) implement reading with enforced file layout.
                throw Error.NotImplemented("Enforced file layout currently not implemented.");
            }

            // 2. Layout
            {
                if (!TryGetFileLayout(in afHeader, out _afLayout))
                    throw ArzError.UnknownLayout();
            }
            if (!_afLayout.IsValid()) throw ArzError.InvalidLayout();

            // 3. Record Type Table
            ArzRecordClassTable afRecordClassTable = new ArzRecordClassTable();

            // 4. Read String Table
            ArzStringTable afStringTable;
            {
                afStream.Seek(afHeader.StringTableOffset);
                afStringTable = afStream
                    .GetStringTableReaderWriter()
                    .Read(expectedSize: afHeader.StringTableSize);
            }

            // Footer
            //ArzFileFooter afFooter;
            //{
            //    afStream.Seek(-ArzFileFooter.Size, IO.SeekOrigin.End);
            //    afStream.ReadFooter(out afFooter);
            //}

            // 5. Read Records
            var arzRecords = new List<ArzRecord>(afHeader.RecordTableCount);
            {
                afStream.Seek(afHeader.RecordTableOffset);
                var afRecordTableReaderWriter = afStream.GetRecordTableReaderWriter(_afLayout, afRecordClassTable);

                int totalBytesRead = 0;
                ArzFileRecord afRecord = default;
                for (var i = 0; i < afHeader.RecordTableCount; i++)
                {
                    afRecordTableReaderWriter.ReadRecord(ref afRecord, out var bytesRead);
                    totalBytesRead += bytesRead;

                    // TODO: (Medium) (ArzFileContext) Validate data offset & data size -> they should be in range of field data area
                    // e.g. between start of block and min(recordTableOffset, stringTableOffset).

                    var arzRecord = new ArzRecord(
                        context: this,
                        nameId: afRecord.NameId,
                        classId: afRecord.ClassId,
                        dataOffset: afRecord.DataOffset,
                        dataSize: afRecord.DataSize,
                        dataSizeDecompressed: afRecord.DataSizeDecompressed,
                        timestamp: afRecord.Timestamp);

                    arzRecords.Add(arzRecord);
                }

                if (totalBytesRead != afHeader.RecordTableSize)
                {
                    throw Error.InvalidOperation("Invalid record table size.");
                }
            }

            // TODO: (Low) (ArzFileContext) Implement stream access checks / contexts
            // TODO: (VeryLow) (ArzFileContext) Count for real number of seeks

            // Field Data Loading
            if (_fieldDataLoadingMode != ArzReadingMode.Lazy)
            {
                IEnumerable<ArzRecord> orderedRecords;
                if (RecordsInOrder(arzRecords))
                {
                    orderedRecords = arzRecords;
                }
                else
                {
                    orderedRecords = arzRecords.OrderBy(x => x.DataOffset);
                }

                if (_fieldDataLoadingMode == ArzReadingMode.Raw)
                {
                    foreach (var record in orderedRecords)
                    {
                        var data = ReadRawFieldData(record.DataOffset, record.DataSize, record.DataSizeDecompressed);
                        record.SetRawFieldDataCore(data);
                    }
                }
                else if (_fieldDataLoadingMode == ArzReadingMode.Full)
                {
                    var effectiveDegreeOfParallelism = MultithreadingHelpers.GetEffectiveDegreeOfParallelism(_maxDegreeOfParallelism);

                    if (_multithreadedReading && effectiveDegreeOfParallelism > 0)
                    {
                        ReadFieldDataMultithreaded(orderedRecords, effectiveDegreeOfParallelism);
                    }
                    else
                    {
                        foreach (var record in orderedRecords)
                        {
                            var data = ReadFieldData(record.DataOffset, record.DataSize, record.DataSizeDecompressed);
                            record.SetFieldDataCore(data);
                        }
                    }
                }
                else throw Error.InvalidOperation("Invalid field data loading mode.");
            }

            // TODO: (Low) (ArzFileContext) (Decision) We might close underlying stream in Raw and Full mode,
            // as we already read all file contents. However, for writing, when we want to save data
            // without re-encoding - then this file will be needed to restore raw data.

            var arzDatabase = new ArzDatabase(this, arzRecords);
            return new InitializeResult(
                stringTable: afStringTable,
                recordClassTable: afRecordClassTable,
                database: arzDatabase);
        }

        private DataBuffer ReadRawFieldDataAsBuffer(int dataOffset, int dataSize, int dataSizeDecompressed)
        {
            if (dataOffset < 0) throw Error.InvalidOperation(nameof(dataOffset)); // TODO: use properexceptions
            if (dataSize < 0) throw Error.InvalidOperation(nameof(dataSize)); // TODO: use properexceptions
            if (dataSizeDecompressed < 0) throw Error.InvalidOperation(nameof(dataSizeDecompressed)); // TODO: use properexceptions
            // TODO: (Low) (ArzFileContext) Validate field data block location, it should be in field data area.

            CheckStreamAccess(StreamAccess.Seek | StreamAccess.Read);

            var afStream = GetArzFileStream();
            var baseOffset = ArzFileHeader.Size;
            afStream.Seek(baseOffset + dataOffset);

            var buffer = DataBuffer.Rent(dataSize);
            var bytesRead = afStream.Read(buffer.Span);
            Check.True(bytesRead == buffer.Length);
            return buffer;
        }

        internal byte[] ReadRawFieldData(int dataOffset, int dataSize, int dataSizeDecompressed)
        {
            if (dataOffset < 0) throw Error.InvalidOperation(nameof(dataOffset)); // TODO: use properexceptions
            if (dataSize < 0) throw Error.InvalidOperation(nameof(dataSize)); // TODO: use properexceptions
            if (dataSizeDecompressed < 0) throw Error.InvalidOperation(nameof(dataSizeDecompressed)); // TODO: use properexceptions
            // TODO: (Low) (ArzFileContext) Validate field data block location, it should be in field data area.

            CheckStreamAccess(StreamAccess.Seek | StreamAccess.Read);

            var afStream = GetArzFileStream();
            var baseOffset = ArzFileHeader.Size;
            afStream.Seek(baseOffset + dataOffset);

            var buffer = Multitargeting.AllocateUninitializedByteArray(dataSize);
            var bytesRead = afStream.Read(buffer);
            Check.True(bytesRead == buffer.Length);
            return buffer;
        }

        internal override DataBuffer ReadRawFieldDataBuffer(int dataOffset, int dataSize, int dataSizeDecompressed)
        {
            // TODO: (Low) (ArzFileContext) This is same as ReadRawFieldData, but uses different memory allocation. Can be unified.

            if (dataOffset < 0) throw Error.InvalidOperation(nameof(dataOffset)); // TODO: use properexceptions
            if (dataSize < 0) throw Error.InvalidOperation(nameof(dataSize)); // TODO: use properexceptions
            if (dataSizeDecompressed < 0) throw Error.InvalidOperation(nameof(dataSizeDecompressed)); // TODO: use properexceptions
            // TODO: (Low) (ArzFileContext) Validate field data block location, it should be in field data area.

            CheckStreamAccess(StreamAccess.Seek | StreamAccess.Read);

            var afStream = GetArzFileStream();
            var baseOffset = ArzFileHeader.Size;
            afStream.Seek(baseOffset + dataOffset);

            var buffer = DataBuffer.Rent(dataSize);
            var bytesRead = afStream.Read(buffer.Span);
            Check.True(bytesRead == buffer.Length);
            return buffer;
        }

        internal override byte[] ReadFieldData(int offset, int compressedSize, int decompressedSize)
        {
            InferCompressionAlgorithm(offset, compressedSize, decompressedSize);

            CheckStreamAccess(StreamAccess.Seek | StreamAccess.Read);

            var afStream = GetArzFileStream();
            afStream.Seek(ArzFileHeader.Size + offset);

            var codec = GetDecoder();
            var decompressedData = codec.Decode(afStream.Stream, compressedSize, decompressedSize);
            return decompressedData;
        }

        internal override byte[] DecodeRawFieldData(Span<byte> source, int decompressedSize)
        {
            InferCompressionAlgorithm(source, decompressedSize);
            var codec = GetDecoder();
            return codec.Decode(source, decompressedSize);
        }

        private Decoder GetDecoder()
        {
            if (_decoder != null) return _decoder;
            else return _decoder = CreateDecoder();
        }

        private Decoder CreateDecoder()
        {
            if ((_afLayout & ArzFileLayout.UseZlibCompression) == ArzFileLayout.UseZlibCompression)
            {
                if (_useLibDeflate)
                {
                    return new ZlibLibDeflateDecoder();
                }
                else
                {
                    return ZlibDecoder.Shared;
                }
            }
            else if ((_afLayout & ArzFileLayout.UseLz4Compression) == ArzFileLayout.UseLz4Compression)
            {
                return Lz4Decoder.Shared;
            }
            else throw Error.InvalidOperation("Unknown compression algorithm.");
        }

        private void InferCompressionAlgorithm(int dataOffset, int dataSize, int decompressedSize)
        {
            if (_afLayout.HasCompressionAlgorithm()) return;
            InferCompressionAlgorithmSlow(dataOffset, dataSize, decompressedSize);
        }

        private unsafe void InferCompressionAlgorithmSlow(int dataOffset, int dataSize, int decompressedSize)
        {
            CheckStreamAccess(StreamAccess.Seek | StreamAccess.Read);

            var afStream = GetArzFileStream();
            afStream.Seek(ArzFileHeader.Size + dataOffset);

            ushort buffer;
            Span<byte> span = new Span<byte>(&buffer, sizeof(ushort));
            var bytesRead = afStream.Read(span);

            InferCompressionAlgorithmSlow(span.Slice(0, bytesRead), decompressedSize);
        }

        private void InferCompressionAlgorithm(ReadOnlySpan<byte> data, int decompressedSize)
        {
            if (_afLayout.HasCompressionAlgorithm()) return;
            InferCompressionAlgorithmSlow(data, decompressedSize);
        }

        private void InferCompressionAlgorithmSlow(ReadOnlySpan<byte> data, int decompressedSize)
        {
            var scoreZlib = 0;
            var scoreLz4 = 0;

            if (!_afLayout.RecordHasDecompressedSize() && decompressedSize == 0)
            {
                // If there is no decompressed size then compressed data
                // can't be LZ4 (which requires to know decompressed
                // size, and it normally stored). So, point against LZ4.
                scoreLz4--;
            }

            if (ZlibUtilities.IsRfc1950StreamHeader(data))
            {
                scoreZlib++;
            }
            else
            {
                scoreLz4++;
            }

            if (scoreZlib > scoreLz4)
            {
                _afLayout |= ArzFileLayout.UseZlibCompression;
            }
            else if (scoreLz4 > scoreZlib)
            {
                _afLayout |= ArzFileLayout.UseLz4Compression;
            }
            else
            {
                throw Error.InvalidOperation("Unable to infer compression algorithm. You should specify it manually.");
            }
        }

        #region File Format Helpers

        // TODO: (ArzFileContext) TryGetFileLayout should be in some helper. Similar method exist in ArzWriter.
        private static bool TryGetFileLayout(in ArzFileHeader header, out ArzFileLayout fileLayout)
        {
            if (header.Magic == 2 && header.Version == 3)
            {
                // Titan Quest, Titan Quest: Immortal Throne
                // Grim Dawn 1.1.7.1
                fileLayout = ArzFileLayout.RecordHasDecompressedSize;
                return true;
            }
            else if (header.Magic == 4 && header.Version == 3)
            {
                // Titan Quest Anniversary Edition (2.9)
                fileLayout = ArzFileLayout.UseZlibCompression;
                return true;
            }
            else
            {
                fileLayout = default;
                return false;
            }
        }

        #endregion

        private void CheckStreamAccess(StreamAccess streamAccess)
        {
            // TODO: (Low) (ArzFileContext) (CheckStreamAccess) Implement this checks correctly, or drop them. 
            // implement stream access checks via struct-based scopes. This would be cheap and pretty effective.
            // (and handly to write with inline `using var scope = ...`). Delay until hybrid writer will appear,
            // or writer which able to modify existing file.

            if (streamAccess == StreamAccess.None)
            {
                throw Error.InvalidOperation("Requested stream access is not valid at this time.");
            }
        }

        private ArzFileStream GetArzFileStream() => new ArzFileStream(GetStream());

        private IO.Stream GetStream() => _stream;

        private static IO.FileStream OpenStream(string path)
        {
            // TODO: (VeryLow) (ArzFileContext) Try open file in sequential scan mode.
            // return new IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, 4096, IO.FileOptions.SequentialScan);
            return IO.File.Open(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read);
        }

        /// <summary>
        /// Check what records are ordered by data offset (so they may be read
        /// with minimum number of seeks).
        /// </summary>
        private static bool RecordsInOrder(List<ArzRecord> records)
        {
            var currentDataOffset = 0;
            foreach (var record in records)
            {
                var dataOffset = record.DataOffset;
                if (currentDataOffset > dataOffset) return false;
                currentDataOffset = dataOffset;
            }
            return true;
        }

        [Flags]
        private enum StreamAccess
        {
            None = 0,
            Seek = 1 << 0,
            Read = 1 << 1,
            Write = 1 << 2,
        }
    }
}
