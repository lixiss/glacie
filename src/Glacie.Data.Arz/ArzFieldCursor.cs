using System;
using System.Runtime.CompilerServices;
using Glacie.Data.Arz.Infrastructure;
using BP = System.Buffers.Binary.BinaryPrimitives;

namespace Glacie.Data.Arz
{
    // TODO: (Low) (ArzFieldCursor) Check inlining - aggressive inlining is required for almost all of this methods.

    // Field Data
    // | Offset     | Data Type | Field             | Note                              |
    // |:----------:|:---------:|:------------------|-----------------------------------|
    // | 0x0000     | int16     | Type              | Type of Value: 0 - int32, 1 - float32, 2 - string (int32 reference to string table), 3 - boolean(also encoded as int32) |
    // | 0x0002     | int16     | ValueCount        | Number of elements in value array |
    // | 0x0004     | int32     | Name              | String index in string table      |
    // | 0x0008     | int32[]   | Values            | Array of values                   |

    internal struct ArzFieldCursor
    {
        private readonly byte[] _data;
        private readonly int _length;
        private int _position;

        public ArzFieldCursor(byte[] data, int length, int position)
        {
            _data = data;
            _length = length;
            _position = position;
        }

        public readonly int Position
        {
            get => _position;
        }

        public readonly bool AtEnd
        {
            get => _position >= _length;
        }

        public readonly bool IsTailChunk
        {
            get => _position + ChunkSize >= _length;
        }

        public void MoveNext()
        {
            _position += ChunkSize;
        }

        public readonly ArzValueType FieldType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (ArzValueType)(
                    BP.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(_data, _position, 2)) & 0x00FF
                    );
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var v = BP.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(_data, _position, 2));
                v = (ushort)((v & 0xFF00) | ((ushort)value & 0xFF));
                BP.WriteUInt16LittleEndian(new Span<byte>(_data, _position, 2), v);
            }
        }

        public bool Removed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                // return (_data[_position + 1] & 0x01) != 0;
                return (BP.ReadInt16LittleEndian(new ReadOnlySpan<byte>(_data, _position, 2)) & 0x0100) != 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set
            {
                var v = BP.ReadInt16LittleEndian(new ReadOnlySpan<byte>(_data, _position, 2));
                if (value) v |= 0x0100;
                else v &= ~0x0100;
                BP.WriteInt16LittleEndian(new Span<byte>(_data, _position, 2), v);
            }
        }

        public readonly ushort ValueCount
        {
            get
            {
                return BP.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(_data, _position + 2, 2));
            }
        }

        public readonly arz_string_id NameId
        {
            get
            {
                return (arz_string_id)BP.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_data, _position + 4, 4));
            }
            set
            {
                BP.WriteInt32LittleEndian(new Span<byte>(_data, _position + 4, 4), (int)value);
            }
        }

        public readonly int RawValue
        {
            get
            {
                DebugCheck.True(ValueCount == 1);
                return BP.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_data, _position + 8, 4));
            }
        }

        public readonly int GetRawValueAt(int index)
        {
            if (index < 0) throw Error.ArgumentOutOfRange(nameof(index));
            if (index >= ValueCount) throw Error.ArgumentOutOfRange(nameof(index));
            return BP.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_data, _position + 8 + 4 * index, 4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetRawValueAtNoCheck(int index)
        {
            return BP.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_data, _position + 8 + 4 * index, 4));
        }

        public readonly int ChunkSize
        {
            get => GetChunkSize(ValueCount);
        }

        public void SetRemoved()
        {
            Check.True(!Removed);

            if (ArzRecord.DefensiveRemoval)
            {
                FieldType = (ArzValueType)0xFF;
                NameId = (arz_string_id)(-1);
            }

            Removed = true;
        }

        public const int MinimumChunkSize = 12;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetChunkSize(int valueCount)
        {
            return 8 + valueCount * 4;
        }

        public int CopyChunk(Span<byte> targetSpan)
        {
            DebugCheck.True(FieldType != ArzValueType.String);

            var chunkSize = ChunkSize;
            var sourceSpan = new ReadOnlySpan<byte>(_data, _position, chunkSize);
            sourceSpan.CopyTo(targetSpan);
            return chunkSize;
        }

        public int CopyChunk(Span<byte> targetSpan, ArzStringEncoder valueEncoder)
        {
            var sourceSpan = new ReadOnlySpan<byte>(_data, _position, MinimumChunkSize);

            var fieldType = (ArzValueType)BP.ReadUInt16LittleEndian(sourceSpan);
            var valueCount = BP.ReadUInt16LittleEndian(sourceSpan.Slice(2));
            var nameId = (arz_string_id)BP.ReadInt32LittleEndian(sourceSpan.Slice(4));

            DebugCheck.True(fieldType <= ArzValueType.Boolean);

            nameId = valueEncoder.Encode(nameId);

            BP.WriteUInt16LittleEndian(targetSpan, (ushort)fieldType);
            BP.WriteUInt16LittleEndian(targetSpan.Slice(2), valueCount);
            BP.WriteInt32LittleEndian(targetSpan.Slice(4), (int)nameId);

            if (valueCount == 1)
            {
                var v = BP.ReadInt32LittleEndian(sourceSpan.Slice(8));
                if (fieldType == ArzValueType.String)
                {
                    v = (int)valueEncoder.Encode((arz_string_id)v);
                }
                BP.WriteInt32LittleEndian(targetSpan.Slice(8), v);
                return MinimumChunkSize;
            }
            else
            {
                sourceSpan = new ReadOnlySpan<byte>(_data, _position + 8, valueCount * 4);
                targetSpan = targetSpan.Slice(8, valueCount * 4);

                if (fieldType != ArzValueType.String)
                {
                    sourceSpan.CopyTo(targetSpan);
                }
                else
                {
                    for (var i = 0; i < valueCount; i++)
                    {
                        var v = BP.ReadInt32LittleEndian(sourceSpan.Slice(i * 4));
                        v = (int)valueEncoder.Encode((arz_string_id)v);
                        BP.WriteInt32LittleEndian(targetSpan.Slice(i * 4), v);
                    }
                }

                return GetChunkSize(valueCount);
            }
        }
    }
}
