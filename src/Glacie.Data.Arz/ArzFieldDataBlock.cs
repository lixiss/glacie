using System;
using System.Runtime.CompilerServices;
using Glacie.Buffers;
using Glacie.Data.Arz.Infrastructure;
using BP = System.Buffers.Binary.BinaryPrimitives;

namespace Glacie.Data.Arz
{
    internal struct ArzFieldDataBlock
    {
        private const int DefaultCapacity = 4;

        private byte[]? _data;
        private int _length;
        // TODO: (Low) (ArzFieldDataBlock) (Decision) collect stats, like number of fields, number of removed fields, etc.?

        public ArzFieldDataBlock(byte[] data, int length)
        {
            _data = data;
            _length = length;
        }

        public DataBuffer AsDataBuffer()
        {
            return DataBuffer.Create(_data!, _length);
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetCursor(out ArzFieldCursor cursor)
        {
            cursor = new ArzFieldCursor(_data!, _length, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetCursor(int position, out ArzFieldCursor cursor)
        {
            DebugCheck.True(position < _length);
            cursor = new ArzFieldCursor(_data!, _length, position);
        }

        public bool HasData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length > 0;
        }

        public int Capacity
        {
            get => _data?.Length ?? 0;
            set
            {
                if (value < _length) throw Error.ArgumentOutOfRange(nameof(value));

                if (value != Capacity)
                {
                    if (value > 0)
                    {
                        byte[] newData = ArrayUtilities.AllocateUninitializedArray<byte>(value);
                        if (_data != null)
                        {
                            Array.Copy(_data, newData, _length);
                        }
                        _data = newData;
                    }
                    else
                    {
                        _data = null;
                    }
                }
            }
        }

        public void TrimExcess()
        {
            if (_data == null) return;

            int threshold = (int)(_data.Length * 0.9);
            if (_length < threshold)
            {
                Capacity = _length;
            }
        }

        public void AssignData(byte[] data, int length)
        {
            _data = data;
            _length = length;
        }

        public Span<byte> Span => new Span<byte>(_data, 0, _length);

        public int Append(arz_string_id nameId, ArzValueType valueType, int rawValue)
        {
            const ushort valueCount = 1;
            const ushort requiredSize = 2 + 2 + 4 + 4;

            var position = _length;

            EnsureCapacity(position + requiredSize);

            var targetSpan = new Span<byte>(_data, position, requiredSize);

            BP.WriteUInt16LittleEndian(targetSpan.Slice(0, 2), (ushort)valueType);
            BP.WriteUInt16LittleEndian(targetSpan.Slice(2, 2), valueCount);
            BP.WriteInt32LittleEndian(targetSpan.Slice(4, 4), (int)nameId);
            BP.WriteInt32LittleEndian(targetSpan.Slice(8), rawValue);

            _length += requiredSize;
            return position;
        }

        public int Append(arz_string_id nameId, ArzValueType valueType, ReadOnlySpan<byte> valueData)
        {
            DebugCheck.True(valueData.Length % 4 == 0);

            var valueCount = checked((ushort)(valueData.Length / 4));
            var requiredSize = 2 + 2 + 4 + valueData.Length;

            var position = _length;

            EnsureCapacity(position + requiredSize);

            var targetSpan = new Span<byte>(_data, position, requiredSize);

            BP.WriteUInt16LittleEndian(targetSpan.Slice(0, 2), (ushort)valueType);
            BP.WriteUInt16LittleEndian(targetSpan.Slice(2, 2), valueCount);
            BP.WriteInt32LittleEndian(targetSpan.Slice(4, 4), (int)nameId);
            valueData.CopyTo(targetSpan.Slice(8));

            _length += requiredSize;
            return position;
        }

        public void Update(int position, ArzValueType valueType, int rawValue)
        {
            var targetSpan = new Span<byte>(_data, position, 12);

            BP.WriteUInt16LittleEndian(targetSpan.Slice(0, 2), (ushort)valueType);
            BP.WriteInt32LittleEndian(targetSpan.Slice(8), rawValue);
        }

        public void Update(int position, ArzValueType valueType, ReadOnlySpan<byte> valueData)
        {
            DebugCheck.True(valueData.Length % 4 == 0);

            var targetSpan = new Span<byte>(_data, position, 2 + 2 + 4 + valueData.Length);

            BP.WriteUInt16LittleEndian(targetSpan.Slice(0, 2), (ushort)valueType);
            valueData.CopyTo(targetSpan.Slice(8));
        }

        public void UpdateForSplit(int position, ArzValueType valueType, ReadOnlySpan<byte> valueData)
        {
            DebugCheck.True(valueData.Length % 4 == 0);

            var valueCount = checked((ushort)(valueData.Length / 4));

            var targetSpan = new Span<byte>(_data, position, 2 + 2 + 4 + valueData.Length);

            BP.WriteUInt16LittleEndian(targetSpan.Slice(0, 2), (ushort)valueType);
            BP.WriteUInt16LittleEndian(targetSpan.Slice(2, 2), valueCount);
            valueData.CopyTo(targetSpan.Slice(8));
        }

        public void UpdateEphemeral(int position, int chunkSize)
        {
            Check.True(chunkSize >= ArzFieldCursor.MinimumChunkSize);

            var targetSpan = new Span<byte>(_data, position, ArzFieldCursor.MinimumChunkSize);

            var valueCount = checked((ushort)((chunkSize - 8) / 4)); // TODO: make helper value count from chunk size

            BP.WriteUInt16LittleEndian(targetSpan.Slice(0, 2), (ushort)0xFFFF); // Invalid type and Removed bit.
            BP.WriteUInt16LittleEndian(targetSpan.Slice(2, 2), valueCount);
            BP.WriteInt32LittleEndian(targetSpan.Slice(4, 4), -1);
            BP.WriteInt32LittleEndian(targetSpan.Slice(8, 4), -1); // TODO: this line is not needed
        }

        public void UpdateTailChunk(int position, ArzValueType valueType, ReadOnlySpan<byte> valueData)
        {
            // TODO: this call valid only on last field
            DebugCheck.True(valueData.Length % 4 == 0);

            var valueCount = checked((ushort)(valueData.Length / 4));
            var requiredSize = 2 + 2 + 4 + valueData.Length;

            EnsureCapacity(position + requiredSize);

            var targetSpan = new Span<byte>(_data, position, requiredSize);

            BP.WriteUInt16LittleEndian(targetSpan.Slice(0, 2), (ushort)valueType);
            BP.WriteUInt16LittleEndian(targetSpan.Slice(2, 2), valueCount);
            valueData.CopyTo(targetSpan.Slice(8));

            _length = position + requiredSize;
        }

        private void EnsureCapacity(int minCapacity)
        {
            var capacity = Capacity;

            if (capacity < minCapacity)
            {
                var newCapacity = capacity == 0 ? DefaultCapacity : capacity * 2;
                if (newCapacity < minCapacity) newCapacity = minCapacity;
                Capacity = newCapacity;
            }
        }
    }
}
