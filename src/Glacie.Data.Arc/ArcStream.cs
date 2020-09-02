using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

using Glacie.Data.Arc.Infrastructure;

using BP = System.Buffers.Binary.BinaryPrimitives;
using IO = System.IO;

namespace Glacie.Data.Arc
{
    internal struct ArcStream
    {
        // Encoding in which all characters in range 0..255 mapped unchanged.
        private static readonly Encoding s_encoding = Encoding.GetEncoding("iso-8859-1");

        private const int ReadStringStackBufferSize = 256;
        private const int WriteStringStackBufferSize = 256;

        private readonly IO.Stream _stream;

        public ArcStream(IO.Stream stream)
        {
            _stream = stream;
        }

        public IO.Stream Stream => _stream;

        #region String

        public string ReadString(int length)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var requiredBufferSize = length + 1;

                Span<byte> buffer =
                    requiredBufferSize <= ReadStringStackBufferSize
                    ? stackalloc byte[ReadStringStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(requiredBufferSize));

                var bytes = buffer.Slice(0, length + 1);
                var bytesRead = _stream.Read(bytes);
                Check.True(bytesRead == bytes.Length);
                if (bytes[length] != 0) throw Error.InvalidOperation("Invalid string. String should be zero-ended.");

                return s_encoding.GetString(bytes.Slice(0, length));
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    // TODO: (VeryLow) (PerformanceMetrics) # of rented buffers by ArcStream::ReadString.
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        public void WriteString(string value, out int length, out int bytesWritten)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var requiredBufferSize = s_encoding.GetByteCount(value) + 1;

                Span<byte> buffer =
                    requiredBufferSize <= WriteStringStackBufferSize
                    ? stackalloc byte[WriteStringStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(requiredBufferSize));

                var actualLength = s_encoding.GetBytes(value, buffer);
                buffer[actualLength] = 0;

                _stream.Write(buffer.Slice(0, actualLength + 1));
                length = actualLength;
                bytesWritten = actualLength + 1;
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    // TODO: (VeryLow) (PerformanceMetrics) # of rented buffers by ArcStream::WriteString.
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        #endregion

        #region Header

        public void ReadHeader(out ArcFileHeader value)
        {
            const int size = ArcFileHeader.Size;

            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            ReadHeader(span, out value);
        }

        public static void ReadHeader(ReadOnlySpan<byte> span, out ArcFileHeader value)
        {
            value = default;
            value.Magic = BP.ReadUInt32LittleEndian(span);
            value.Version = BP.ReadInt32LittleEndian(span.Slice(4));
            value.EntryCount = BP.ReadInt32LittleEndian(span.Slice(8));
            value.ChunkCount = BP.ReadInt32LittleEndian(span.Slice(12));
            value.ChunkTableLength = BP.ReadUInt32LittleEndian(span.Slice(16));
            value.StringTableLength = BP.ReadUInt32LittleEndian(span.Slice(20));
            value.ChunkTableOffset = BP.ReadUInt32LittleEndian(span.Slice(24));
        }

        public void WriteHeader(in ArcFileHeader value)
        {
            Span<byte> span = stackalloc byte[ArcFileHeader.Size];
            WriteHeader(span, value);
            _stream.Write(span);
        }

        public static void WriteHeader(Span<byte> span, in ArcFileHeader value)
        {
            BP.WriteUInt32LittleEndian(span, value.Magic);
            BP.WriteInt32LittleEndian(span.Slice(4), value.Version);
            BP.WriteInt32LittleEndian(span.Slice(8), value.EntryCount);
            BP.WriteInt32LittleEndian(span.Slice(12), value.ChunkCount);
            BP.WriteUInt32LittleEndian(span.Slice(16), value.ChunkTableLength);
            BP.WriteUInt32LittleEndian(span.Slice(20), value.StringTableLength);
            BP.WriteUInt32LittleEndian(span.Slice(24), value.ChunkTableOffset);
        }

        #endregion

        #region Entry

        public void ReadEntry(out ArcFileEntry value)
        {
            const int size = ArcFileEntry.Size;

            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            ReadEntry(span, out value);
        }

        public static void ReadEntry(ReadOnlySpan<byte> span, out ArcFileEntry value)
        {
            value = default;
            value.EntryType = (ArcFileEntryType)BP.ReadUInt32LittleEndian(span);
            value.Offset = BP.ReadUInt32LittleEndian(span.Slice(4));
            value.CompressedLength = BP.ReadUInt32LittleEndian(span.Slice(8));
            value.Length = BP.ReadUInt32LittleEndian(span.Slice(12));
            value.Hash = BP.ReadUInt32LittleEndian(span.Slice(16));
            value.Timestamp = BP.ReadInt64LittleEndian(span.Slice(20));
            value.ChunkCount = BP.ReadInt32LittleEndian(span.Slice(28));
            value.ChunkIndex = BP.ReadInt32LittleEndian(span.Slice(32));
            value.NameStringLength = BP.ReadInt32LittleEndian(span.Slice(36));
            value.NameStringOffset = BP.ReadUInt32LittleEndian(span.Slice(40));
        }

        public void WriteEntry(in ArcFileEntry value)
        {
            Span<byte> span = stackalloc byte[ArcFileEntry.Size];
            WriteEntry(span, value);
            _stream.Write(span);
        }

        public static void WriteEntry(Span<byte> span, in ArcFileEntry value)
        {
            BP.WriteUInt32LittleEndian(span, (uint)value.EntryType);
            BP.WriteUInt32LittleEndian(span.Slice(4), value.Offset);
            BP.WriteUInt32LittleEndian(span.Slice(8), value.CompressedLength);
            BP.WriteUInt32LittleEndian(span.Slice(12), value.Length);
            BP.WriteUInt32LittleEndian(span.Slice(16), value.Hash);
            BP.WriteInt64LittleEndian(span.Slice(20), value.Timestamp);
            BP.WriteInt32LittleEndian(span.Slice(28), value.ChunkCount);
            BP.WriteInt32LittleEndian(span.Slice(32), value.ChunkIndex);
            BP.WriteInt32LittleEndian(span.Slice(36), value.NameStringLength);
            BP.WriteUInt32LittleEndian(span.Slice(40), value.NameStringOffset);
        }

        #endregion

        #region ArcChunk

        public void ReadChunk(out ArcEntryChunk value)
        {
            const int size = ArcEntryChunk.Size;

            Span<byte> span = stackalloc byte[size];
            var bytesRead = _stream.Read(span);
            Check.True(bytesRead == size);
            ReadChunk(span, out value);
        }

        public static void ReadChunk(ReadOnlySpan<byte> span, out ArcEntryChunk value)
        {
            value = default;
            value.Offset = BP.ReadUInt32LittleEndian(span);
            value.CompressedLength = BP.ReadInt32LittleEndian(span.Slice(4));
            value.Length = BP.ReadInt32LittleEndian(span.Slice(8));
        }

        public void WriteChunk(in ArcEntryChunk value)
        {
            Span<byte> span = stackalloc byte[ArcEntryChunk.Size];
            WriteChunk(span, value);
            _stream.Write(span);
        }

        public static void WriteChunk(Span<byte> span, in ArcEntryChunk value)
        {
            BP.WriteUInt32LittleEndian(span, value.Offset);
            BP.WriteInt32LittleEndian(span.Slice(4), value.CompressedLength);
            BP.WriteInt32LittleEndian(span.Slice(8), value.Length);
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

        public long Seek(long offset, IO.SeekOrigin origin = IO.SeekOrigin.Begin)
        {
            if (origin == IO.SeekOrigin.Begin)
            {
                var position = _stream.Position;
                if (position == offset) return position;
            }

            return _stream.Seek(offset, origin);
        }

        #endregion
    }
}
