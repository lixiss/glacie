using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Glacie.Data.Arz.Infrastructure;
using BP = System.Buffers.Binary.BinaryPrimitives;
using IO = System.IO;

namespace Glacie.Data.Arz.FileFormat
{
    // TODO: (Lixiss) (ArzFileStream) I'm not very happy with this helper.

    public readonly struct ArzFileStream
    {
        // Encoding in which all characters in range 0..255 mapped unchanged.
        private static readonly Encoding s_encoding = Encoding.GetEncoding("iso-8859-1");

        private const int ReadStringStackBufferSize = 256;
        private const int WriteStringStackBufferSize = 256;

        private readonly IO.Stream _stream;

        public ArzFileStream(IO.Stream stream)
        {
            _stream = stream;
        }

        public IO.Stream Stream => _stream;

        #region Primitives

        public ushort ReadUInt16()
        {
            const int size = 2;
            // TODO: (High) (ArzFileStream) instead of stackallock use local variables and unsafe.
            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            return BP.ReadUInt16LittleEndian(span);
        }

        public int ReadInt32()
        {
            const int size = 4;
            // TODO: (High) (ArzFileStream) instead of stackallock use local variables and unsafe.
            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            return BP.ReadInt32LittleEndian(span);
        }

        public int ReadInt32(out int bytesRead)
        {
            const int size = 4;
            // TODO: (High) (ArzFileStream) instead of stackallock use local variables and unsafe.
            Span<byte> span = stackalloc byte[size];
            bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            return BP.ReadInt32LittleEndian(span);
        }

        public void WriteInt32(int value)
        {
            const int size = 4;
            // TODO: (High) (ArzFileStream) instead of stackallock use local variables and unsafe.
            Span<byte> span = stackalloc byte[size];
            BP.WriteInt32LittleEndian(span, value);
            _stream.Write(span);
        }

        public long ReadInt64()
        {
            const int size = 8;
            // TODO: (High) (ArzFileStream) instead of stackallock use local variables and unsafe.
            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            return BP.ReadInt64LittleEndian(span);
        }

        #endregion

        #region String

        public string ReadString()
        {
            byte[]? rentedBuffer = null;
            try
            {
                var length = ReadInt32();
                if (length == 0) return string.Empty;

                Span<byte> buffer =
                    length <= ReadStringStackBufferSize
                    ? stackalloc byte[ReadStringStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));

                var bytes = buffer.Slice(0, length);
                var bytesRead = _stream.Read(bytes);
                Check.True(bytesRead == bytes.Length);

                return s_encoding.GetString(bytes);
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    // TODO: (VeryLow) (PerformanceMetrics) # of rented buffers by ArzFileStream::ReadString.
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        public string ReadString(out int bytesRead)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var length = ReadInt32(out bytesRead);
                if (length == 0) return string.Empty;

                Span<byte> buffer =
                    length <= ReadStringStackBufferSize
                    ? stackalloc byte[ReadStringStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));

                var bytes = buffer.Slice(0, length);
                var br = _stream.Read(bytes);
                bytesRead += br;
                Check.True(br == bytes.Length);

                return s_encoding.GetString(bytes);
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    // TODO: (VeryLow) (PerformanceMetrics) # of rented buffers by ArzFileStream::ReadString.
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        public void WriteString(string? value)
        {
            value ??= "";

            byte[]? rentedBuffer = null;
            try
            {
                var requiredBufferSize = 4 + s_encoding.GetByteCount(value);

                Span<byte> buffer =
                    requiredBufferSize <= WriteStringStackBufferSize
                    ? stackalloc byte[WriteStringStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(requiredBufferSize));

                BP.WriteInt32LittleEndian(buffer, value.Length);
                var actualLength = s_encoding.GetBytes(value, buffer.Slice(4));

                _stream.Write(buffer.Slice(0, actualLength + 4));
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    // TODO: (VeryLow) (PerformanceMetrics) # of rented buffers by ArzFileStream::WriteString.
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        #endregion

        #region Header

        public void ReadHeader(out ArzFileHeader value)
        {
            const int size = ArzFileHeader.Size;

            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            ReadHeader(span, out value);
        }

        public static void ReadHeader(ReadOnlySpan<byte> span, out ArzFileHeader value)
        {
            value = default;
            value.Magic = BP.ReadUInt16LittleEndian(span);
            value.Version = BP.ReadUInt16LittleEndian(span.Slice(2));
            value.RecordTableOffset = BP.ReadInt32LittleEndian(span.Slice(4));
            value.RecordTableSize = BP.ReadInt32LittleEndian(span.Slice(8));
            value.RecordTableCount = BP.ReadInt32LittleEndian(span.Slice(12));
            value.StringTableOffset = BP.ReadInt32LittleEndian(span.Slice(16));
            value.StringTableSize = BP.ReadInt32LittleEndian(span.Slice(20));
        }

        public void WriteHeader(in ArzFileHeader value)
        {
            Span<byte> span = stackalloc byte[ArzFileHeader.Size];
            WriteHeader(span, value);
            _stream.Write(span);
        }

        public static void WriteHeader(Span<byte> span, in ArzFileHeader value)
        {
            BP.WriteUInt16LittleEndian(span, value.Magic);
            BP.WriteUInt16LittleEndian(span.Slice(2), value.Version);
            BP.WriteInt32LittleEndian(span.Slice(4), value.RecordTableOffset);
            BP.WriteInt32LittleEndian(span.Slice(8), value.RecordTableSize);
            BP.WriteInt32LittleEndian(span.Slice(12), value.RecordTableCount);
            BP.WriteInt32LittleEndian(span.Slice(16), value.StringTableOffset);
            BP.WriteInt32LittleEndian(span.Slice(20), value.StringTableSize);
        }

        #endregion

        #region Footer

        public void ReadFooter(out ArzFileFooter value)
        {
            const int size = ArzFileFooter.Size;

            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            ReadFooter(span, out value);
        }

        public static void ReadFooter(ReadOnlySpan<byte> span, out ArzFileFooter value)
        {
            value = default;
            value.Hash = BP.ReadUInt32LittleEndian(span);
            value.StringTableHash = BP.ReadUInt32LittleEndian(span.Slice(4));
            value.RecordDataHash = BP.ReadUInt32LittleEndian(span.Slice(8));
            value.RecordTableHash = BP.ReadUInt32LittleEndian(span.Slice(12));
        }

        public void WriteFooter(in ArzFileFooter value)
        {
            Span<byte> span = stackalloc byte[ArzFileFooter.Size];
            WriteFooter(span, value);
            _stream.Write(span);
        }

        public static void WriteFooter(Span<byte> span, in ArzFileFooter value)
        {
            BP.WriteUInt32LittleEndian(span, value.Hash);
            BP.WriteUInt32LittleEndian(span.Slice(4), value.StringTableHash);
            BP.WriteUInt32LittleEndian(span.Slice(8), value.RecordDataHash);
            BP.WriteUInt32LittleEndian(span.Slice(12), value.RecordTableHash);
        }

        #endregion

        #region Record Table

        public RecordTableReaderWriter GetRecordTableReaderWriter(ArzFileFormat format, ArzRecordClassTable recordClassTable)
            => new RecordTableReaderWriter(this, format, recordClassTable);

        public readonly struct RecordTableReaderWriter
        {
            private readonly ArzFileStream _afStream;
            private readonly ArzFileFormat _format;
            private readonly ArzRecordClassTable _recordClassTable;

            public RecordTableReaderWriter(ArzFileStream afStream, ArzFileFormat format, ArzRecordClassTable recordClassTable)
            {
                _afStream = afStream;
                _format = format;
                _recordClassTable = recordClassTable;
            }

            public ArzFileRecord[] ReadArray(int count, int? expectedSize)
            {
                var actualSize = 0;

                var records = new ArzFileRecord[count];
                for (int i = 0; i < count; i++)
                {
                    ReadRecord(ref records[i], out var bytesRead);
                    actualSize += bytesRead;
                }

                // Ensure what table was completely read.
                if (expectedSize.HasValue && actualSize != expectedSize)
                {
                    throw Error.InvalidOperation("Expected size of record table is {0}, but was consumed {1} bytes.", expectedSize, actualSize);
                }

                return records;
            }

            public void ReadRecord(ref ArzFileRecord value, out int bytesRead)
            {
                // NameId     int32
                // Type       string
                // DataOffset int32
                // DataSize   int32
                // DataSizeDecompressed  int32  -- only if fileLayout.RecordHasDecompressedSize
                // Timestamp  int64

                value.NameId = (arz_string_id)_afStream.ReadInt32();

                // TypeId
                {
                    var @class = _afStream.ReadString(out bytesRead);
                    var classId = _recordClassTable.GetOrAdd(@class);
                    value.ClassId = classId;
                }

                Span<byte> span = stackalloc byte[4 + 4 + 4 + 8];

                if (!_format.RecordHasDecompressedLength)
                {
                    span = span.Slice(0, 4 + 4 + 8);
                }

                {
                    var spanBytesRead = _afStream.Read(span);
                    Check.True(spanBytesRead == span.Length);
                    bytesRead += 4 + spanBytesRead;
                }

                value.DataOffset = BP.ReadInt32LittleEndian(span);
                value.DataSize = BP.ReadInt32LittleEndian(span.Slice(4));

                if (_format.RecordHasDecompressedLength)
                {
                    value.DataSizeDecompressed = BP.ReadInt32LittleEndian(span.Slice(8));
                    value.Timestamp = BP.ReadInt64LittleEndian(span.Slice(12));
                }
                else
                {
                    value.DataSizeDecompressed = 0;
                    value.Timestamp = BP.ReadInt64LittleEndian(span.Slice(8));
                }
            }

            public void WriteRecord(in ArzFileRecord value)
            {
                // NameIndex  int32
                // Type       string
                // DataOffset int32
                // DataSize   int32
                // DataSizeDecompressed  int32  -- only if fileLayout.RecordHasDecompressedSize
                // Timestamp  int64

                _afStream.WriteInt32((int)value.NameId);
                _afStream.WriteString(_recordClassTable[value.ClassId]);

                Span<byte> span = stackalloc byte[20]; // 4 + 4 + 4 + 8

                if (!_format.RecordHasDecompressedLength)
                {
                    span = span.Slice(0, 16); // 4 + 4 + 8
                }

                BP.WriteInt32LittleEndian(span, value.DataOffset);
                BP.WriteInt32LittleEndian(span.Slice(4), value.DataSize);

                if (_format.RecordHasDecompressedLength)
                {
                    var decompressedSize = _format.MustSetDecompressedLength ? value.DataSizeDecompressed : 0;
                    BP.WriteInt32LittleEndian(span.Slice(8), decompressedSize);
                    BP.WriteInt64LittleEndian(span.Slice(12), value.Timestamp);
                }
                else
                {
                    BP.WriteInt64LittleEndian(span.Slice(8), value.Timestamp);
                }

                _afStream.Write(span);
            }
        }

        #endregion

        #region String Table

        public StringTableReaderWriter GetStringTableReaderWriter() => new StringTableReaderWriter(this);

        public readonly struct StringTableReaderWriter
        {
            private readonly ArzFileStream _afStream;

            public StringTableReaderWriter(ArzFileStream afStream)
            {
                _afStream = afStream;
            }

            public ArzStringTable Read(int? expectedSize)
            {
                var values = ReadStringList(expectedSize);
                return new ArzStringTable(values, takeOwnership: true);
            }

            // TODO: (Low) (ArzFileStream) (StringTableReaderWriter) - Do we really need perform size validation?
            // It may be better to return number of bytes read.

            private List<string> ReadStringList(int? expectedSize)
            {
                var actualSize = 0;

                var numberOfStrings = _afStream.ReadInt32(out var bytesRead);
                actualSize += bytesRead;

                var result = new List<string>(numberOfStrings);
                for (var i = 0; i < numberOfStrings; i++)
                {
                    var value = _afStream.ReadString(out bytesRead);
                    result.Add(value);

                    actualSize += bytesRead;
                }

                // Ensure what table was completely read.
                if (expectedSize.HasValue && actualSize != expectedSize.Value)
                {
                    throw Error.InvalidOperation("Expected size of string table is {0}, but was consumed {1} bytes.", expectedSize, actualSize);
                }

                return result;
            }

            public void Write(ArzStringTable stringTable)
            {
                _afStream.WriteInt32(stringTable.Count);

                foreach (var x in stringTable.GetValues())
                {
                    _afStream.WriteString(x);
                }
            }
        }

        #endregion

        #region Stream

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(Span<byte> buffer)
        {
            return _stream.Read(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> buffer)
        {
            _stream.Write(buffer);
        }

        /// <summary>
        /// When stream already at requested offset - it is not need to seek
        /// over stream again and again.
        /// </summary>
        /// <remarks>
        /// `FileStream` has underlying buffering, and random seeking costs
        /// becomes bit bigger. However most of our reads are sequential so it
        /// works perfectly. However, seeking also has additional checks, buffer
        /// management, but somewhy this simple case doesn't handled in simpliest
        /// way, as result this simple hack speed-up records reading up-to
        /// 20-30% (exactly very similar result gives preloading data into
        /// memory stream, but this doesn't need to allocate nothing).
        /// Note, that files doesn't has any requirements about record sorting,
        /// and some of GD files has few records which out of sort
        /// (seems they are not properly compacted).
        /// </remarks>
        public long Seek(long offset, IO.SeekOrigin origin = IO.SeekOrigin.Begin)
        {
            if (origin == IO.SeekOrigin.Begin)
            {
                var position = _stream.Position;
                if (position == offset) return position;

                // TODO: (Low) (ArzFileStream::Seek) - i want get number of seeks.
                // Need a way to collect this information as per-context performance metric.
            }

            return _stream.Seek(offset, origin);
        }

        #endregion
    }
}
