using System;
using System.Collections.Generic;
using Glacie.Data.Arz.FileFormat;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Data.Arz.Utilities;
using Glacie.Data.Compression;
using IO = System.IO;

namespace Glacie.Data.Arz
{
    // TODO: (Medium) (ArzWriter) Make writer into .dbr and .gxr files.

    // TODO: (Low) (ArzWriter) (Decision) do we need editing an existing file functionality, or prefer create always new file?
    // generally hybrid like saves are should be easy with already opened file in writable mode...

    // TODO: (Low) Need progress over actions... for whole library.

    /// <summary>
    /// Writes content of <see cref="ArzDatabase"/> into <c>.arz</c> file.
    /// </summary>
    public sealed partial class ArzWriter : IDisposable
    {
        #region API

        public static void Write(string path, ArzDatabase database, ArzWriterOptions? options = null)
        {
            using var stream = IO.File.Open(path, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite, IO.FileShare.None);
            Write(stream, database, options);
        }

        public static void Write(IO.Stream stream, ArzDatabase database, ArzWriterOptions? options = null)
        {
            using var writer = new ArzWriter(database, options ?? new ArzWriterOptions());
            writer.Write(stream);
        }

        #endregion

        private readonly ArzDatabase _database;

        private Encoder? _encoder;

        // options
        private readonly bool _multithreadedWriting;
        private readonly int _maxDegreeOfParallelism;
        private readonly bool _useLibDeflate;
        private ArzFileLayout _afLayout;
        private readonly bool _inferRecordClass;
        private readonly bool _changesOnly;
        private bool _forceCompression;
        private bool _optimizeStringTable;
        private readonly bool _calculateChecksum;
        private readonly int _compressionLevel;

        // state
        private ArzFileStream _afStream;
        private ArzStringEncoder? _afStringEncoder;
        private ArzFileRecord[] _afRecords;
        private int _afRecordIndex;
        private int _afRecordDataOffset;
        private bool _afStringTableIsCompatibleWithRawFieldData;
        private long _modifiedTimestamp;
        private ArzContext _context;

        private ArzWriter(ArzDatabase database, ArzWriterOptions options)
        {
            _database = database ?? throw Error.ArgumentNull(nameof(database));

            _changesOnly = options.ChangesOnly;
            _multithreadedWriting = options.Multithreaded;
            _maxDegreeOfParallelism = options.MaxDegreeOfParallelism;
            _useLibDeflate = options.UseLibDeflate ?? ZlibLibDeflateEncoder.IsSupported;
            _afLayout = options.Layout;
            _inferRecordClass = options.InferRecordClass;
            _optimizeStringTable = options.OptimizeStringTable;
            _forceCompression = options.ForceCompression;
            _calculateChecksum = options.ComputeChecksum;
            _compressionLevel = options.CompressionLevel;
        }

        public void Dispose()
        {
            _encoder?.Dispose();

            _afStream = default;
            _afStringEncoder = null!;
            _afRecords = null!;
            _context = null!;
        }

        public void Write(IO.Stream stream)
        {
            CheckStream(stream);
            InferAndCheckLayout();
            AdjustLayoutDependentOptions();

            // Header with magic and version
            ArzFileHeader afHeader;
            if (!TryCreateHeaderForLayout(_afLayout, out afHeader))
            {
                throw Error.InvalidOperation("Can't create header for file layout.");
            }

            _context = _database.Context;
            var sourceStringTable = _context.StringTable;

            ArzStringTable afStringTable;
            if (_optimizeStringTable)
            {
                // TODO: (VeryLow) (ArzWriter) (Undecided) We can try detect if string table needs to be optimized.
                // Just make array map similar to ArzStringEncoder and mark string usage
                // by indexes. In this way we can determine if there is present strings
                // which is not in use, and how many strings. If this check will be 
                // very fast -> this probably can be more profitable. Also may be good 
                // heuristic for hybrid mode.

                afStringTable = new ArzStringTable(sourceStringTable.Count);
                _afStringEncoder = new ArzStringEncoder(sourceStringTable, afStringTable);
            }
            else
            {
                afStringTable = sourceStringTable;
                _afStringEncoder = null;
            }

            _afStringTableIsCompatibleWithRawFieldData =
                afStringTable == sourceStringTable;

            // Validate Records
            var outputRecords = new List<ArzRecord>(_database.Count);
            {
                foreach (var record in _database.GetAll())
                {
                    var recordClassId = record.ClassId;
                    if (recordClassId == 0)
                    {
                        if (_inferRecordClass)
                        {
                            // TODO: (Medium) (ArzWriter) At this stage we doesn't want trigger re-creating of field map.
                            // Add this service into context and check in record.
                            // Also try to finish this operation in single op / may be as internal call.
                            if (record.TryGet(WellKnownFieldNames.Class, out var classField))
                            {
                                if (classField.ValueType != ArzValueType.String && classField.Count != 1)
                                {
                                    throw ArzError.FieldTypeMismatch("Field \"Class\" must be a string.");
                                }

                                record.Class = classField.Get<string>();
                            }
                            else
                            {
                                throw ArzError.RecordHasNoClass(record.Name);
                            }
                        }
                        else throw ArzError.RecordHasNoClass(record.Name);
                    }

                    if (!record.Any()) throw ArzError.RecordHasNoAnyField(record.Name);

                    var isModified = record.IsNew || record.IsModified || record.IsDataModified;
                    if (_changesOnly && !isModified)
                        continue;

                    outputRecords.Add(record);
                }
            }

            // Header
            _afStream = new ArzFileStream(stream);
            _afStream.Seek(0);
            _afStream.WriteHeader(in afHeader);

            // Record Table
            var recordWriter = _afStream.GetRecordTableReaderWriter(_afLayout, _context.RecordClassTable);

            // Produce Records:
            // It should process record one by one and provide record data
            // Record data might be in compressed form, or in decompressed form
            // After what consumers should encode this data and then
            // Write them out in output (another producer-consumer pipeline).
            // So generally we should take not modified records, and then modified...
            // However currently let's just out some data to file...

            _modifiedTimestamp = DateTime.UtcNow.ToFileTimeUtc();

            _afRecords = new ArzFileRecord[outputRecords.Count];
            _afRecordDataOffset = 0;
            _afRecordIndex = 0;

            if (_multithreadedWriting)
            {
                var effectiveDegreeOfParallelism = MultithreadingHelpers.GetEffectiveDegreeOfParallelism(_maxDegreeOfParallelism);
                WriteRecordsMultithreaded(outputRecords, effectiveDegreeOfParallelism);
            }
            else
            {
                WriteRecordsSinglethreaded(outputRecords);
            }

            var afRecordsCount = _afRecordIndex;
            Check.True(_afRecordIndex == _afRecords.Length);

            // Record Table
            afHeader.RecordTableCount = afRecordsCount;
            afHeader.RecordTableOffset = (int)stream.Position;
            for (var i = 0; i < afRecordsCount; i++)
            {
                recordWriter.WriteRecord(in _afRecords[i]);
            }
            afHeader.RecordTableSize = (int)stream.Position - afHeader.RecordTableOffset;

            // String Table
            afHeader.StringTableOffset = (int)stream.Position;
            _afStream.GetStringTableReaderWriter().Write(afStringTable);
            afHeader.StringTableSize = (int)stream.Position - afHeader.StringTableOffset;

            var footerPosition = stream.Position;

            // Header
            _afStream.Seek(0);
            _afStream.WriteHeader(in afHeader);

            // Footer
            ArzFileFooter afFooter;
            if (_calculateChecksum)
            {
                _afStream.Seek(0);
                ArzChecksum.Compute(_afStream.Stream, footerPosition, out afFooter);
            }
            else
            {
                afFooter = default;
            }
            _afStream.Seek(footerPosition);
            _afStream.WriteFooter(in afFooter);

            // Trim
            stream.SetLength(stream.Position);
            _afStream.Seek(0);

            // TODO: (Low) (ArzWriter) Add metrics.
            //Console.WriteLine("   Source String Table: {0}", sourceStringTable.Count);
            //Console.WriteLine("Optimized String Table: {0} ({1})", afStringTable.Count, afStringTable.Count - sourceStringTable.Count);
        }

        // TODO: (Low) (ArzWriter) This is reverse of ArzFileContext::TryGetFileLayout - move them into some helper.
        private static bool TryCreateHeaderForLayout(ArzFileLayout layout, out ArzFileHeader header)
        {
            if ((layout & ArzFileLayout.RecordHasDecompressedSize) != 0)
            {
                if ((layout & ArzFileLayout.UseLz4Compression) != 0)
                {
                    // Grim Dawn 1.1.7.1
                    header = new ArzFileHeader
                    {
                        Magic = 2,
                        Version = 3,
                    };
                    return true;
                }
                else if ((layout & ArzFileLayout.UseZlibCompression) != 0)
                {
                    // Titan Quest
                    // Titan Quest: Immortal Throne
                    header = new ArzFileHeader
                    {
                        Magic = 2,
                        Version = 3,
                    };
                    return true;
                }
            }
            else
            {
                if ((layout & ArzFileLayout.UseZlibCompression) != 0)
                {
                    // Titan Quest Anniversary Edition (2.9)
                    header = new ArzFileHeader
                    {
                        Magic = 4,
                        Version = 3,
                    };
                    return true;
                }
            }

            header = default;
            return false;
        }

        private static void CheckStream(IO.Stream stream)
        {
            if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
            {
                throw Error.InvalidOperation("This operation requires stream which can read, write and seek.");
            }
        }

        private void InferAndCheckLayout()
        {
            if (_afLayout == ArzFileLayout.None)
            {
                // TODO: (Low) (ArzWriter) (Undecided) Force layout inference is work, but not sure what this should exist.
                var layout = _database.Context.Layout;
                if (!layout.IsComplete() && _database.Context.HasLayoutInference)
                {
                    foreach (var record in _database.GetAll())
                    {
                        if (!record.IsNew)
                        {
                            // Trigger field counting, so record will be loaded.
                            var fieldCount = record.Count;
                            layout = _database.Context.Layout;
                            break;
                        }
                    }
                }
                _afLayout = layout;
            }

            if (_afLayout == ArzFileLayout.None)
                throw ArzError.LayoutRequired("Layout has not been inferred from database. You should specify it manually.");

            if (!_afLayout.IsValid())
                throw ArzError.InvalidLayout();

            if (!_afLayout.IsComplete())
                throw ArzError.LayoutRequired("Incomplete layout. You should specify layout in writer options.");
        }

        private void AdjustLayoutDependentOptions()
        {
            // When source and target files has different compression algorithms,
            // we should re-encode everything.
            if (!_afLayout.CompressionAlgorithmCompatibleWith(_database.Context.Layout))
            {
                _forceCompression = true;
            }
        }

        private Encoder GetEncoder()
        {
            if (_encoder != null) return _encoder;
            else return _encoder = CreateEncoder();
        }

        private Encoder CreateEncoder()
        {
            if ((_afLayout & ArzFileLayout.UseZlibCompression) == ArzFileLayout.UseZlibCompression)
            {
                if (_useLibDeflate)
                {
                    return new ZlibLibDeflateEncoder(_compressionLevel);
                }
                else
                {
                    return new ZlibEncoder(_compressionLevel);
                }
            }
            else if ((_afLayout & ArzFileLayout.UseLz4Compression) == ArzFileLayout.UseLz4Compression)
            {
                return new Lz4Encoder(_compressionLevel);
            }
            else throw Error.InvalidOperation("Unknown compression algorithm.");
        }
    }
}
