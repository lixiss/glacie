using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glacie.Abstractions;
using Glacie.Buffers;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Utilities;

namespace Glacie.Data.Arz
{
    using FieldMap = Dictionary<arz_string_id, arz_field_ptr>;

    // TODO: (Low) (ArzRecord) (Decision) Automatically assign record class.
    // - Consider to automatically call SetClass whenever "Class" field also assigned (which performance cost for this?)
    // - This doesn't greatly fits into overall idea behind ArzRecord. Glacie.Context will better handle this.

    public sealed class ArzRecord : IArzRecord, IArzRecordMetrics,
        IRecordApi,
        IRecordFieldCollectionApi<ArzField, ArzField?, ArzField>
    {
        // On-stack buffer size for value encoding.
        private const int SetOrAddInternalOnStackBufferSize = 64 * 4;

        // If enabled, then field map might be used. Otherwise it is never created.
        // Useful for debugging purposes.
        private const bool FieldMapFeatureEnabled = true;

        // When enabled - field map always created regardless to internal counters.
        // Useful for debugging purposes.
        private const bool ForceFieldMap = false;

        // When field is marked as removed -> also put some non-valid values for type and name.
        internal const bool DefensiveRemoval = true;

        // 16 seems perfect value when under heavy field lookup by name we still
        // doesn't get worse results. However this load-dependent and may be
        // other self-profiling techniques might be more useful.
        // For example this might be not optimal if we process all records and get field from it.
        private const int UseNameMapWhenCountGreaterThan = 16;
        private const int BuildNameMapAfterNumberOfRequests = 10;
        private const int UseNameMapAfterNumberOfNonMappedFieldByNameCalls = 10;

        private readonly ArzContext _context;

        private RecordFlags _flags;

        // Access to this field should be preceded by LoadFieldDataIfNeed.
        private ArzFieldDataBlock _fdb0;

        // ARZ record, identity
        private readonly arz_string_id _nameId;
        private int _classId;

        // ARZ record, field data reference
        // This value are valid when is not a new record (e.g. loaded from file).
        private int _dataOffset;
        private int _dataSize;
        private int _dataSizeDecompressed;

        // ARZ record, metadata
        // This value are valid when is not a new record (e.g. loaded from file).
        private long _timestamp;

        // Holds number of fields.
        // This field is valid only when HasFieldCount flag is set.
        // If this field is set - all operations should maintain this value.
        private int _fieldCount;

        // Holds mapping between name_id and arz_field_ptr in field data block.
        private WeakReference<FieldMap>? _fieldMapWeakRef;
        private int _numberOfFieldMapRequests;
        private int _numberOfNonMappedFieldByNameCalls;

        private int _numberOfRemovedFields;
        private int _version;

        /// <summary>
        /// Create new record which is not linked to a field data block.
        /// </summary>
        internal ArzRecord(ArzContext context, arz_string_id nameId, int classId)
        {
            _context = context;
            _nameId = nameId;
            _classId = classId;
            _fieldCount = 0;
            _flags = RecordFlags.New
                | RecordFlags.DataModified // TODO: consider to not use this flag here
                | RecordFlags.HasCount
                | RecordFlags.FieldData;
        }

        /// <summary>
        /// Create existing record which is linked to a field data block.
        /// </summary>
        internal ArzRecord(ArzContext context,
            arz_string_id nameId,
            int classId,
            int dataOffset,
            int dataSize,
            int dataSizeDecompressed,
            long timestamp)
        {
            _context = context;
            _nameId = nameId;
            _classId = classId;
            _dataOffset = dataOffset;
            _dataSize = dataSize;
            _dataSizeDecompressed = dataSizeDecompressed;
            _timestamp = timestamp;
            _flags = 0;
        }

        #region ArzWriter

        internal arz_string_id NameId => _nameId;
        internal int ClassId => _classId;

        /// <summary>
        /// Returns true when record has any fields.
        /// Main difference from checking <c>Count &gt; 0</c>, what this will
        /// not require field counting.
        /// </summary>
        internal bool Any()
        {
            if (HasCount) { return _fieldCount > 0; }
            else
            {
                if (HasFieldData)
                {
                    return HasAnyFieldSlow();
                }
                else if (!IsNew)
                {
                    // Assume that when record loaded from file always has fields.
                    return true;
                }
                else throw Error.Unreachable();
            }
        }

        /// <summary>
        /// Returns compacted field data block.
        /// </summary>
        internal DataBuffer GetFieldDataBuffer(bool loadFieldData)
        {
            if (loadFieldData) { LoadFieldDataIfNeed(); }

            // Call of this method when no or raw field data present is invalid.
            Check.True(HasFieldData);

            if (IsDataModified) Compact();

            Check.True(_numberOfRemovedFields == 0);

            // TODO: (VeryLow) (ArzRecord) Looks like we always return here only shared buffer (buffer owned by record),
            // so api can be changed, in favor to reflect what there is nothing to return into pool. (E.g. just return array and length).
            return _fdb0.AsDataBuffer();
        }

        internal DataBuffer GetRawFieldDataBuffer()
        {
            Check.True(HasRawFieldData);
            return _fdb0.AsDataBuffer();
        }

        internal DataBuffer EncodeFieldDataBuffer(ArzStringEncoder afStringEncoder, bool pool)
        {
            Check.True(afStringEncoder != null);

            LoadFieldDataIfNeed();

            // TODO: (Low) (ArzRecord) EncodeFieldDataBuffer: if there is exist some large number of removed records,
            // we might calculate exact size first, especially for non-pooled version used for record importing.
            DataBuffer outputBuffer;
            if (pool)
            {
                outputBuffer = DataBuffer.Rent(_fdb0.Length);
            }
            else
            {
                outputBuffer = DataBuffer.Create(_fdb0.Length);
            }

            var outputPosition = 0;

            _fdb0.GetCursor(out var c);

            while (!c.AtEnd)
            {
                if (!c.Removed)
                {
                    outputPosition += c.CopyChunk(
                        outputBuffer.Span.Slice(outputPosition),
                        afStringEncoder
                        );
                }
                c.MoveNext();
            }

            return outputBuffer.WithLength(outputPosition);
        }

        private void Compact()
        {
            if (_numberOfRemovedFields == 0) return;

            Check.True(HasFieldData);

            Check.True(_numberOfRemovedFields > 0);
            Check.True(_fdb0.HasData);

            // TODO: (Low) (ArzRecord) (Undecided) I'm might replace _numberOfRemovedFields to removed data size
            // and use some threshold to trigger not just compacting over existing
            // buffer, but compacting into new buffer. This will be similar to trim-excess
            // but may end with better characteristics. This not so good for saving
            // but it might be not bad during editing... anyway metrics needed.

            // TODO: (Low) (ArzRecord) (Undecided) in-place compacting is good when we doesn't want allocate
            // new memory but there is also an option to allocate new buffer
            // to minimum required size and we will get trimmed buffer almost
            // for free. However this still might require to get some stats to
            // get benefits.

            // TODO: (Low) (ArzRecord) (Undecided) another option is to drop field map during save, as we most
            // likely will not have chance reuse it, so compacting without map
            // updating might be more profitable.

            var fieldMap = GetFieldMap();
            if (fieldMap != null)
            {
                CompactWithFieldMap(fieldMap);
            }
            else
            {
                CompactWithoutFieldMap();
            }

            // _fdb0.TrimExcess();

            _version++;
            _numberOfRemovedFields = 0;
        }

        private void CompactWithoutFieldMap()
        {
            var buffer = _fdb0.AsDataBuffer();
            var sourceArray = buffer.Array;
            var targetArray = buffer.Array;
            var targetPosition = 0;

            _fdb0.GetCursor(out var c);
            while (true)
            {
                // Skip removed fields.
                while (!c.AtEnd)
                {
                    if (c.Removed) c.MoveNext();
                    else break;
                }
                if (c.AtEnd) break;

                var sourcePosition = c.Position;
                // Skip alive fields.
                while (!c.AtEnd)
                {
                    if (!c.Removed) c.MoveNext();
                    else break;
                }

                var sourceLength = c.Position - sourcePosition;

                DebugCheck.True(c.Position >= targetPosition + sourceLength);

                if (sourcePosition != targetPosition || sourceArray != targetArray)
                {
                    Array.Copy(sourceArray, sourcePosition, targetArray, targetPosition, sourceLength);
                }
                targetPosition += sourceLength;

                if (c.AtEnd) break;
            }

            _fdb0.AssignData(targetArray, targetPosition);
        }

        private void CompactWithFieldMap(FieldMap fieldMap)
        {
            var buffer = _fdb0.AsDataBuffer();
            var sourceArray = buffer.Array;
            var targetArray = buffer.Array;
            var targetPosition = 0;

            _fdb0.GetCursor(out var c);
            while (true)
            {
                // Skip removed fields.
                while (!c.AtEnd)
                {
                    if (c.Removed) c.MoveNext();
                    else break;
                }
                if (c.AtEnd) break;

                // Process non-removed fields one-by-one.
                while (!c.AtEnd)
                {
                    if (!c.Removed)
                    {
                        var nameId = c.NameId;

                        var sourcePosition = c.Position;
                        c.MoveNext();
                        var sourceLength = c.Position - sourcePosition;

                        DebugCheck.True(c.Position >= targetPosition + sourceLength);

                        if (sourcePosition != targetPosition || sourceArray != targetArray)
                        {
                            Array.Copy(sourceArray, sourcePosition, targetArray, targetPosition, sourceLength);

                            DebugCheck.True(fieldMap[nameId] == MakeFieldPtr(sourcePosition));
                            fieldMap[nameId] = MakeFieldPtr(targetPosition);
                        }
                        targetPosition += sourceLength;
                    }
                    else break;
                }

                if (c.AtEnd) break;
            }

            _fdb0.AssignData(targetArray, targetPosition);
        }

        #endregion

        #region Import & Adopt

        /// <remarks>
        /// This method doesn't changes record name or record class - caller should do this.
        /// </remarks>
        internal void ImportFrom(ArzRecord externalRecord, ArzStringEncoder stringEncoder)
        {
            var rdb = externalRecord.EncodeFieldDataBuffer(stringEncoder, pool: false);
            AssignDataAndProperties(rdb, externalRecord.Timestamp);
        }

        private void AssignDataAndProperties(DataBuffer dataBuffer, long timestamp)
        {
            DebugCheck.True(!dataBuffer.Owned);

            _flags = RecordFlags.New
                | RecordFlags.FieldData;

            _fdb0.AssignData(dataBuffer.Array, dataBuffer.Length);

            // Don't touch this fields: callers should do this.
            // _nameId
            // _classId

            _dataOffset = 0;
            _dataSize = 0;
            _dataSizeDecompressed = 0;

            _timestamp = timestamp;

            _fieldCount = 0;

            ClearFieldMap();
            _numberOfFieldMapRequests = 0;
            _numberOfNonMappedFieldByNameCalls = 0;

            _numberOfRemovedFields = 0;
            _version++;
        }

        #endregion

        #region Context

        internal ArzContext Context
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _context;
        }

        internal ArzDatabase Database
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _context.Database;
        }

        internal ArzStringTable StringTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _context.StringTable;
        }

        internal ArzRecordClassTable RecordClassTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _context.RecordClassTable;
        }

        #endregion

        #region API

        /// <summary>
        /// Returns internal record version.
        /// </summary>
        public int Version => _version;

        // TODO: (Medium) (ArzRecord) Reorganize file layout. It mixes everything now...

        public string Name => StringTable[_nameId];

        public string Class
        {
            get => RecordClassTable[_classId];
            set
            {
                _classId = RecordClassTable.GetOrAdd(value);
                _version++;
                _flags |= RecordFlags.Modified;
            }
        }

        public long Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                _flags |= RecordFlags.ExplicitTimestamp | RecordFlags.Modified;
            }
        }

        /// <summary>
        /// Gets or sets the last time the record in the database was changed.
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

        public int Count => HasCount ? _fieldCount : GetCountSlow();

        public IEnumerable<ArzField> SelectAll()
        {
            // TODO: (Low) (ArzRecord) Create specialized struct-based field enumerator instead of yield return.
            LoadFieldDataIfNeed();
            var currentVersion = _version;

            _fdb0.GetCursor(out var c);
            while (!c.AtEnd)
            {
                if (!c.Removed)
                {
                    yield return new ArzField(this, MakeFieldPtr(c.Position));
                }

                if (currentVersion != _version) throw Error.InvalidOperation("Collection was modified; enumeration operation may not execute.");
                c.MoveNext();
            }
        }

        public bool TryGet(string name, out ArzField value)
        {
            // TODO: (Low) (ArzRecord) Unsure about TryGet / TryGetFieldCursor

            if (TryGetFieldCursor(name, out var c))
            {
                value = new ArzField(this, MakeFieldPtr(c.Position));
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet(string name, ArzRecordOptions options, out ArzField value)
        {
            // TODO: (Low) (ArzRecord) Unsure about TryGet / TryGetFieldCursor

            if (TryGetFieldCursor(name, options, out var c))
            {
                value = new ArzField(this, MakeFieldPtr(c.Position));
                return true;
            }

            value = default;
            return false;
        }

        public ArzField Get(string name)
        {
            if (TryGet(name, out var value))
            {
                return value;
            }
            else throw ArzError.FieldNotFound(name);
        }

        public ArzField Get(string name, ArzRecordOptions options)
        {
            if (TryGet(name, options, out var value))
            {
                return value;
            }
            else throw ArzError.FieldNotFound(name);
        }

        public ArzField? GetOrNull(string name)
        {
            if (TryGet(name, out var value))
            {
                return value;
            }
            else return null;
        }

        public ArzField? GetOrNull(string name, ArzRecordOptions options)
        {
            if (TryGet(name, options, out var value))
            {
                return value;
            }
            else return null;
        }

        // TODO: make both methods to use shared logic
        private bool TryGetFieldCursor(string name, out ArzFieldCursor cursor)
        {
            if (StringTable.TryGet(name, out var nameId))
            {
                var fieldMap = GetOrCreateFieldMap();
                if (fieldMap != null)
                {
                    if (fieldMap.TryGetValue(nameId, out var fieldPtr))
                    {
                        _fdb0.GetCursor(GetPositionFromFieldPtr(fieldPtr), out cursor);
                        return !cursor.Removed;
                    }
                }
                else
                {
                    _numberOfNonMappedFieldByNameCalls++;

                    LoadFieldDataIfNeed();
                    _fdb0.GetCursor(out var c);
                    while (!c.AtEnd)
                    {
                        if (!c.Removed && c.NameId == nameId)
                        {
                            cursor = c;
                            return true;
                        }

                        c.MoveNext();
                    }
                }
            }

            cursor = default;
            return false;
        }

        private bool TryGetFieldCursor(string name, ArzRecordOptions options, out ArzFieldCursor cursor)
        {
            if (StringTable.TryGet(name, out var nameId))
            {
                var fieldMap = GetFieldMap(options);
                if (fieldMap != null)
                {
                    if (fieldMap.TryGetValue(nameId, out var fieldPtr))
                    {
                        _fdb0.GetCursor(GetPositionFromFieldPtr(fieldPtr), out cursor);
                        return !cursor.Removed;
                    }
                }
                else
                {
                    _numberOfNonMappedFieldByNameCalls++;

                    LoadFieldDataIfNeed();
                    _fdb0.GetCursor(out var c);
                    while (!c.AtEnd)
                    {
                        if (!c.Removed && c.NameId == nameId)
                        {
                            cursor = c;
                            return true;
                        }

                        c.MoveNext();
                    }
                }
            }

            cursor = default;
            return false;
        }

        public void Set(string name, int value)
            => SetRawValue(name, ArzValueType.Integer, value);

        public void Set(string name, float value)
        {
            if (Features.ThrowOnNonFiniteValues)
            {
                ArzBitConverter.CheckFinite(value);
            }
            SetRawValue(name, ArzValueType.Real, ArzBitConverter.Float32ToInt32(value));
        }

        public void Set(string name, double value)
        {
            if (Features.ThrowOnNonFiniteValues)
            {
                ArzBitConverter.CheckFinite(value);
            }
            SetRawValue(name, ArzValueType.Real, ArzBitConverter.Float64ToInt32(value));
        }

        public void Set(string name, bool value)
            => SetRawValue(name, ArzValueType.Boolean, value ? 1 : 0);

        public void Set(string name, string value)
            => SetRawValue(name, ArzValueType.String, (int)StringTable.GetOrAdd(value));

        public void Set(string name, int[] value)
        {
            if (value.Length == 1)
            {
                Set(name, value[0]);
            }
            else
            {
                if (BitConverter.IsLittleEndian)
                {
                    SetOrAddRaw(name, ArzValueType.Integer,
                        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value[0], value.Length)),
                        throwOnAdd: false);
                }
                else
                {
                    SetOrAddInternal(name, value, throwOnAdd: false);
                }
            }
        }

        public void Set(string name, float[] value)
        {
            if (value.Length == 1)
            {
                Set(name, value[0]);
            }
            else
            {
                if (BitConverter.IsLittleEndian)
                {
                    if (Features.ThrowOnNonFiniteValues)
                    {
                        for (var i = 0; i < value.Length; i++)
                        {
                            ArzBitConverter.CheckFinite(value[i]);
                        }
                    }

                    SetOrAddRaw(name, ArzValueType.Real,
                        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value[0], value.Length)),
                        throwOnAdd: false);
                }
                else
                {
                    SetOrAddInternal(name, value, throwOnAdd: false);
                }
            }
        }

        public void Set(string name, double[] value)
        {
            if (value.Length == 1)
            {
                Set(name, value[0]);
            }
            else
            {
                SetOrAddInternal(name, value, throwOnAdd: false);
            }
        }

        public void Set(string name, bool[] value)
        {
            if (value.Length == 1)
            {
                Set(name, value[0]);
            }
            else
            {
                SetOrAddInternal(name, value, throwOnAdd: false);
            }
        }

        public void Set(string name, string[] value)
        {
            if (value.Length == 1)
            {
                Set(name, value[0]);
            }
            else
            {
                SetOrAddInternal(name, value, throwOnAdd: false);
            }
        }

        // TODO: (Low) (ArzRecord) Add/Set from ValueTuples.

        public void Set(string name, Variant value)
        {
            // TODO: (Low) (ArzRecord) pass variant by ref (in)?

            // TODO: (Medium) (ArzRecord) this code is not optimal
            // we already know variant type, and after variant's type check,
            // we can read variant's raw / single value.
            // However need check what JIT do.

            switch (value.Type)
            {
                case VariantType.Integer:
                    Set(name, value.Get<int>());
                    break;

                case VariantType.Real:
                    Set(name, value.Get<float>());
                    break;

                case VariantType.Boolean:
                    Set(name, value.Get<bool>());
                    break;

                case VariantType.String:
                    Set(name, value.Get<string>());
                    break;

                case VariantType.IntegerArray:
                    Set(name, value.Get<int[]>());
                    break;

                case VariantType.RealArray:
                    Set(name, value.Get<float[]>());
                    break;

                case VariantType.BooleanArray:
                    Set(name, value.Get<bool[]>());
                    break;

                case VariantType.StringArray:
                    Set(name, value.Get<string[]>());
                    break;

                case VariantType.Float64Array:
                    Set(name, value.Get<double[]>());
                    break;

                default:
                    throw Error.Argument(nameof(value));
            }
        }

        public Variant this[string name]
        {
            get => GetFieldValueAsVariant(name);
            set => Set(name, value);
        }

        // TODO: (Medium) (ArzRecord) Unsure about Variant this[string name, Variant defaultValue].
        [Obsolete("Not yet implemented. Not sure if it should be implemented.", true)]
        public Variant this[string name, Variant defaultValue]
        {
            get => throw Error.NotImplemented();
        }

        // TODO: (Medium) (ArzRecord) Unsure about GetValueOrDefault(string name).
        [Obsolete("Not yet implemented. Not sure if it should be implemented.", true)]
        public Variant GetValueOrDefault(string name)
        {
            throw Error.NotImplemented();
        }

        // TODO: (Medium) (ArzRecord) Unsure about GetValueOrDefault(string name, Variant defaultValue).
        [Obsolete("Not yet implemented. Not sure if it should be implemented.", true)]
        public Variant GetValueOrDefault(string name, Variant defaultValue)
        {
            throw Error.NotImplemented();
        }

        // TODO: (Medium) (ArzRecord) Add calls is not implemented.

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, int value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, float value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, double value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, bool value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, string value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, int[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, float[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, double[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, bool[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, string[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Add(string name, Variant value) => throw Error.NotImplemented();

        // TODO: (Medium) (ArzRecord) Don't use simple forwards.
        public bool Remove(string name) => RemoveImpl(name);

        [Obsolete("Not yet implemented.", true)]
        public bool Remove(ArzField field) => throw Error.NotImplemented();

        #endregion

        #region Internal API

        // TODO: (Medium) (ArzRecord) Add debug checks when access to DataOffset, DataSize, DataSizeDecompressed, Timestamp. Rename them.
        internal int DataOffset => _dataOffset;
        internal int DataSize => _dataSize;
        internal int DataSizeDecompressed => _dataSizeDecompressed;

        // TODO: (Low) (ArzRecord) SetRawFieldDataCore & SetFieldDataCore methods do same work, only need to setup different flags.
        internal void SetRawFieldDataCore(byte[] data)
        {
            if (data == null) throw Error.ArgumentNull(nameof(data));

            if (HasNoFieldData)
            {
                // NoData -> RawData
                _fdb0.AssignData(data, data.Length);
                SetFieldDataState(RecordFlags.RawFieldData);
            }
            else throw Error.InvalidOperation("Record in invalid state for this operation.");
        }

        internal void SetFieldDataCore(byte[] data)
        {
            if (data == null) throw Error.ArgumentNull(nameof(data));

            if (HasNoFieldData)
            {
                SetFieldDataCoreInternal(data);
            }
            else throw Error.InvalidOperation("Record in invalid state for this operation.");
        }

        #endregion

        #region Field API (Internal)

        // TODO: (Low) (ArzRecord) (Undecided) may be better introduce FieldRef struct, which will act similar to Span (and convertible)?

        internal string GetFieldName(arz_field_ptr fieldPtr)
        {
            var nameId = GetFieldNameId(fieldPtr);
            return StringTable[nameId];
        }

        internal arz_string_id GetFieldNameId(arz_field_ptr fieldPtr)
        {
            return GetFieldCursor(fieldPtr).NameId;
        }

        internal ArzValueType GetFieldValueType(arz_field_ptr fieldPtr)
        {
            return GetFieldCursor(fieldPtr).FieldType;
        }

        internal int GetFieldValueCount(arz_field_ptr fieldPtr)
        {
            return GetFieldCursor(fieldPtr).ValueCount;
        }

        // TODO: (Low) (ArzRecord) This method should use out parameter.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ArzFieldCursor GetFieldCursor(arz_field_ptr fieldPtr)
        {
            _fdb0.GetCursor(GetPositionFromFieldPtr(fieldPtr), out var c);
            return c;
        }

        private void ClearFieldMap()
        {
            if (_fieldMapWeakRef != null)
            {
                _fieldMapWeakRef.SetTarget(null!);
            }
        }

        /// <summary>
        /// Get field map according to record options.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FieldMap? GetFieldMap(ArzRecordOptions options)
        {
            if (_fieldMapWeakRef != null)
            {
                if (_fieldMapWeakRef.TryGetTarget(out var value))
                {
                    return value;
                }
            }

            if ((options & ArzRecordOptions.NoFieldMap) != 0)
            {
                return null;
            }
            else
            {
                return MaybeCreateFieldMap();
            }
        }

        /// <summary>
        /// Get field map but only if it already created.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FieldMap? GetFieldMap()
        {
            if (_fieldMapWeakRef != null)
            {
                if (_fieldMapWeakRef.TryGetTarget(out var value))
                {
                    return value;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FieldMap? GetOrCreateFieldMap()
        {
            return GetFieldMap() ?? MaybeCreateFieldMap();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private FieldMap? MaybeCreateFieldMap()
        {
            _numberOfFieldMapRequests++;

            // TODO: (Low) (ArzRecord) (Undecided) FieldMap creation heuristics need to be verified.
            // Count is not a great logic actually. Instead of counting we can use estimated count (_fdb.Length / 12).

            if (FieldMapFeatureEnabled
                && (ForceFieldMap
                || (_numberOfNonMappedFieldByNameCalls > UseNameMapAfterNumberOfNonMappedFieldByNameCalls)
                || (_numberOfFieldMapRequests > BuildNameMapAfterNumberOfRequests)
                || (Count > UseNameMapWhenCountGreaterThan)))
            {
                // TODO: (Low) (ArzRecord) (Undecided) make stats for creating maps. Count effectively may force to be map always
                // get used, and with layered item processing it might be not a effecient.
                // E.g. in one loop we triggered map to be all maps created, and shortly after this
                // GC may collect them. In second loop we again trigger maps, and again might rebuild
                // field maps.
                // This probably can be avoided by: iterating over records with maps before records
                // without maps. (However this effectively requires sorting/allocations.)
                // Use more strict caching.
                // Alternatively - we must process every item as much as possible, but this may
                // require more complex setup (in Gx). But this should be one of the best.

                var fieldMap = CreateFieldMap();

                if (_fieldMapWeakRef == null)
                {
                    _fieldMapWeakRef = new WeakReference<FieldMap>(fieldMap);
                }
                else
                {
                    _fieldMapWeakRef.SetTarget(fieldMap);
                }

                return fieldMap;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private FieldMap CreateFieldMap()
        {
            LoadFieldDataIfNeed();

            var fieldNameMap = new FieldMap();

            _fdb0.GetCursor(out var c);
            while (!c.AtEnd)
            {
                if (!c.Removed)
                {
                    fieldNameMap.Add(c.NameId, MakeFieldPtr(c.Position));
                }

                c.MoveNext();
            }

            return fieldNameMap;
        }

        internal T GetFieldValue<T>(arz_field_ptr fieldPtr)
        {
            var fc = GetFieldCursor(fieldPtr);
            if (fc.ValueCount != 1) throw ArzError.FieldNotSingleValue();

            if (typeof(T) == typeof(float))
            {
                if (fc.FieldType != ArzValueType.Real)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(ArzBitConverter.Int32ToFloat32(fc.RawValue));
            }
            else if (typeof(T) == typeof(bool))
            {
                if (fc.FieldType != ArzValueType.Boolean)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(fc.RawValue != 0);
            }
            else if (typeof(T) == typeof(int))
            {
                if (fc.FieldType != ArzValueType.Integer)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(fc.RawValue);
            }
            else if (typeof(T) == typeof(string))
            {
                if (fc.FieldType != ArzValueType.String)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(StringTable[(arz_string_id)fc.RawValue]);
            }
            else if (typeof(T) == typeof(double))
            {
                if (fc.FieldType != ArzValueType.Real)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(double)(ArzBitConverter.Int32ToFloat32(fc.RawValue));
            }
            else if (typeof(T) == typeof(Variant))
            {
                // TODO: (High) (ArzRecord) GetFieldValue<Variant> somewhy throw exception. We already done this functionality, so need verify this.
                throw Error.NotImplemented();
            }
            else throw Error.InvalidOperation("Unsupported type.");
        }

        internal int GetRawFieldValue(arz_field_ptr fieldPtr, int index)
        {
            if (index < 0) throw Error.ArgumentOutOfRange(nameof(index));

            var fc = GetFieldCursor(fieldPtr);
            if (index >= fc.ValueCount) throw Error.ArgumentOutOfRange(nameof(index));

            return fc.GetRawValueAtNoCheck(index);
        }

        internal T GetFieldValue<T>(arz_field_ptr fieldPtr, int index)
        {
            if (index < 0) throw Error.ArgumentOutOfRange(nameof(index));

            var fc = GetFieldCursor(fieldPtr);
            if (index >= fc.ValueCount) throw Error.ArgumentOutOfRange(nameof(index));

            if (typeof(T) == typeof(float))
            {
                if (fc.FieldType != ArzValueType.Real)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(ArzBitConverter.Int32ToFloat32(fc.GetRawValueAtNoCheck(index)));
            }
            else if (typeof(T) == typeof(bool))
            {
                if (fc.FieldType != ArzValueType.Boolean)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(fc.GetRawValueAtNoCheck(index) != 0);
            }
            else if (typeof(T) == typeof(int))
            {
                if (fc.FieldType != ArzValueType.Integer)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(fc.GetRawValueAtNoCheck(index));
            }
            else if (typeof(T) == typeof(string))
            {
                if (fc.FieldType != ArzValueType.String)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(StringTable[(arz_string_id)fc.GetRawValueAtNoCheck(index)]);
            }
            else if (typeof(T) == typeof(double))
            {
                if (fc.FieldType != ArzValueType.Real)
                {
                    throw ArzError.FieldTypeMismatch();
                }
                return (T)(object)(double)(ArzBitConverter.Int32ToFloat32(fc.GetRawValueAtNoCheck(index)));
            }
            else throw Error.InvalidOperation("Unsupported type.");
        }

        private Variant GetFieldValueAsVariant(string name)
        {
            // TODO: (Medium) (ArzRecord) GetFieldValueAsVariant uses weird Variant ctors.
            if (TryGetFieldCursor(name, out var c))
            {
                var valueCount = c.ValueCount;
                if (valueCount == 1)
                {
                    switch (c.FieldType)
                    {
                        case ArzValueType.Real:
                            return new Variant(VariantType.Real, c.RawValue, null);

                        case ArzValueType.Boolean:
                            return new Variant(VariantType.Boolean, c.RawValue, null);

                        case ArzValueType.Integer:
                            return new Variant(VariantType.Integer, c.RawValue, null);

                        case ArzValueType.String:
                            return new Variant(VariantType.String, 0, StringTable[(arz_string_id)c.RawValue]);

                        default: throw Error.Unreachable();
                    }
                }
                else
                {
                    // TODO: (Medium) (ArzRecord) GetFieldValueAsVariant for arrays uses weird logic.
                    switch (c.FieldType)
                    {
                        case ArzValueType.Real:
                            {
                                var values = new float[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = ArzBitConverter.Int32ToFloat32(c.GetRawValueAtNoCheck(i));
                                }
                                return new Variant(VariantType.RealArray, 0, values);
                            }

                        case ArzValueType.Boolean:
                            {
                                var values = new bool[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = c.GetRawValueAtNoCheck(i) != 0;
                                }
                                return new Variant(VariantType.BooleanArray, 0, values);
                            }

                        case ArzValueType.Integer:
                            {
                                var values = new int[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = c.GetRawValueAtNoCheck(i);
                                }
                                return new Variant(VariantType.IntegerArray, 0, values);
                            }

                        case ArzValueType.String:
                            {
                                var values = new string[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = StringTable[(arz_string_id)c.GetRawValueAtNoCheck(i)];
                                }
                                return new Variant(VariantType.StringArray, 0, values);
                            }

                        default: throw Error.Unreachable();
                    }
                }
            }

            throw ArzError.FieldNotFound(name);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LoadFieldDataIfNeed()
        {
            if (HasFieldData) return;
            LoadFieldDataSlow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LoadFieldDataSlow()
        {
            Check.True(!IsNew);

            byte[] fieldData;
            if (HasNoFieldData)
            {
                fieldData = _context.ReadFieldData(_dataOffset, _dataSize, _dataSizeDecompressed);
            }
            else if (HasRawFieldData)
            {
                fieldData = _context.DecodeRawFieldData(_fdb0.Span, _dataSizeDecompressed);
            }
            else throw Error.InvalidOperation();

            SetFieldDataCoreInternal(fieldData);
        }

        private void SetFieldDataCoreInternal(byte[] fieldData)
        {
            ArzFieldDataCodec.ValidateFieldDataSize(fieldData);

            // TODO: (VeryLow) (ArzRecord) consider to perform full field data validation,
            // this might include field counting, verify field type and for
            // strings it might check what string references are valid.
            // As result this also might provide field index.

            _fdb0.AssignData(fieldData, fieldData.Length);
            SetFieldDataState(RecordFlags.FieldData);
        }

        // Count number of fields, and updates aggregated value.
        private int GetCountSlow()
        {
            LoadFieldDataIfNeed();

            var count = 0;
            var removedFieldsCount = 0;

            _fdb0.GetCursor(out var c);
            while (!c.AtEnd)
            {
                if (!c.Removed)
                {
                    count++;
                }
                else
                {
                    removedFieldsCount++;
                }
                c.MoveNext();
            }

            _flags |= RecordFlags.HasCount;
            _fieldCount = count;
            _numberOfRemovedFields = removedFieldsCount;
            return count;
        }

        private bool HasAnyFieldSlow()
        {
            Check.That(HasFieldData);

            _fdb0.GetCursor(out var c);
            while (!c.AtEnd)
            {
                if (!c.Removed)
                {
                    return true;
                }
                c.MoveNext();
            }
            return false;
        }

        // TODO: (VeryLow) (ArzRecord) SetRawValue_ForSingleValue_Obsoleting currently not in use.
        // This method was created as fast path, but needs to keep in sync with span-based method.
        // It is unclear if this will be profitable to have separate method, need benchmarking.
        [Obsolete("SetOrAddRaw is too complex to keep two implementations.", true)]
        private void SetRawValue_ForSingleValue_Obsoleting(string name, ArzValueType valueType, int value)
        {
            // There is old and unsynced implementation. Need validate it.

            LoadFieldDataIfNeed();
            const int valueCount = 1;
            var nameId = StringTable.GetOrAdd(name);

            var updated = false;
            var found = false;
            var append = false;

            var fieldMap = GetOrCreateFieldMap();
            if (fieldMap != null)
            {
                if (fieldMap.TryGetValue(nameId, out var fieldPtr))
                {
                    _fdb0.GetCursor(GetPositionFromFieldPtr(fieldPtr), out var c);
                    DebugCheck.True(c.NameId == nameId);

                    if (c.ValueCount == valueCount)
                    {
                        // Found field, update it's value.
                        _fdb0.Update(c.Position, valueType, value);
                        // ensure what it has no removed flag...

                        found = true;
                        updated = true;
                    }
                    else throw Error.NotImplemented();
                }
                else
                {
                    // Field is not found in map. This is should be a new field.
                    append = true; // _numberOfRemovedFields == 0;
                }
            }

            // If map exist - lookup field over map
            // Otherwise add it to block
            if (!found && !append)
            {
                _fdb0.GetCursor(out var c);
                while (!c.AtEnd)
                {
                    // TODO: just split them to both. Without map we should
                    // scan linearly regardless to hit states.

                    // TODO: !!! here we should search for any empty space, name is
                    // actually no matter.
                    // But, if we go without field map - there we should look
                    // for non-removed field by name.
                    if (!c.Removed && c.NameId == nameId)
                    {
                        if (c.ValueCount == valueCount)
                        {
                            // Found field and it's value can be replaced.
                            _fdb0.Update(c.Position, valueType, value);

                            // TODO: alternatively we might update by cursor...
                            //c.FieldType = ArzValueType.Integer;
                            //c.RawValue = value;

                            updated = true;
                            found = true;
                            break;
                        }
                        else throw Error.NotImplemented();
                    }

                    c.MoveNext();
                }
            }

            if (!found)
            {
                // alternatively - Append may return cursor, and all updates
                // may go thru cursor...
                var fieldPosition = _fdb0.Append(nameId, valueType, value);
                if (fieldMap != null)
                {
                    fieldMap.Add(nameId, MakeFieldPtr(fieldPosition));
                }
                if (HasCount) _fieldCount++;
                updated = true;
            }

            Check.True(updated);

            _version++;
            _flags |= RecordFlags.DataModified;
        }

        private void SetRawValue(string name, ArzValueType valueType, int value)
        {
            SetOrAddRaw(name,
                valueType,
                MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)),
                throwOnAdd: false);
        }

        #region SetOrAddInternal

        // TODO: (Low) (ArzRecord) Find a way for make generalized SetOrAddInternal to drop off buffering logic in single method.

        private void SetOrAddInternal(string name, int[] values, bool throwOnAdd)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var length = values.Length * 4;
                Check.True(length > 0);

                Span<byte> buffer =
                    length <= SetOrAddInternalOnStackBufferSize
                    ? stackalloc byte[SetOrAddInternalOnStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));

                var bytes = buffer.Slice(0, length);

                // Encoding
                for (var i = 0; i < values.Length; i++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(i * 4),
                        values[i]);
                }
                SetOrAddRaw(name, ArzValueType.Integer, bytes, throwOnAdd);
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        private void SetOrAddInternal(string name, float[] values, bool throwOnAdd)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var length = values.Length * 4;
                Check.True(length > 0);

                Span<byte> buffer =
                    length <= SetOrAddInternalOnStackBufferSize
                    ? stackalloc byte[SetOrAddInternalOnStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));

                var bytes = buffer.Slice(0, length);

                // Encoding
                for (var i = 0; i < values.Length; i++)
                {
                    if (Features.ThrowOnNonFiniteValues)
                    {
                        ArzBitConverter.CheckFinite(values[i]);
                    }

                    BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(i * 4),
                        ArzBitConverter.Float32ToInt32(values[i]));
                }
                SetOrAddRaw(name, ArzValueType.Real, bytes, throwOnAdd);
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        private void SetOrAddInternal(string name, double[] values, bool throwOnAdd)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var length = values.Length * 4;
                Check.True(length > 0);

                Span<byte> buffer =
                    length <= SetOrAddInternalOnStackBufferSize
                    ? stackalloc byte[SetOrAddInternalOnStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));

                var bytes = buffer.Slice(0, length);

                // Encoding
                for (var i = 0; i < values.Length; i++)
                {
                    if (Features.ThrowOnNonFiniteValues)
                    {
                        ArzBitConverter.CheckFinite(values[i]);
                    }

                    BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(i * 4),
                        ArzBitConverter.Float64ToInt32(values[i]));
                }
                SetOrAddRaw(name, ArzValueType.Real, bytes, throwOnAdd);
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        private void SetOrAddInternal(string name, bool[] values, bool throwOnAdd)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var length = values.Length * 4;
                Check.True(length > 0);

                Span<byte> buffer =
                    length <= SetOrAddInternalOnStackBufferSize
                    ? stackalloc byte[SetOrAddInternalOnStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));

                var bytes = buffer.Slice(0, length);

                // Encoding
                for (var i = 0; i < values.Length; i++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(i * 4),
                        values[i] ? 1 : 0);
                }
                SetOrAddRaw(name, ArzValueType.Boolean, bytes, throwOnAdd);
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        private void SetOrAddInternal(string name, string[] values, bool throwOnAdd)
        {
            byte[]? rentedBuffer = null;
            try
            {
                var length = values.Length * 4;
                Check.True(length > 0);

                Span<byte> buffer =
                    length <= SetOrAddInternalOnStackBufferSize
                    ? stackalloc byte[SetOrAddInternalOnStackBufferSize]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length));

                var bytes = buffer.Slice(0, length);

                // Encoding
                for (var i = 0; i < values.Length; i++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(i * 4),
                        (int)StringTable.GetOrAdd(values[i]));
                }
                SetOrAddRaw(name, ArzValueType.String, bytes, throwOnAdd);
            }
            finally
            {
                if (!(rentedBuffer is null))
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
                }
            }
        }

        #endregion

        // TOOD: need somehow decompose this method...
#if NETSTANDARD2_1
#else
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        private void SetOrAddRaw(string name, ArzValueType valueType, ReadOnlySpan<byte> valueData, bool throwOnAdd)
        {
            LoadFieldDataIfNeed();

            Check.True(valueData.Length % 4 == 0);
            var valueCount = valueData.Length / 4;
            var nameId = StringTable.GetOrAdd(name);

            var fieldMap = GetOrCreateFieldMap();
            if (fieldMap != null)
            {
                if (fieldMap.TryGetValue(nameId, out var fieldPtr))
                {
                    _fdb0.GetCursor(GetPositionFromFieldPtr(fieldPtr), out var c);
                    DebugCheck.True(!c.Removed);
                    DebugCheck.True(c.NameId == nameId);

                    SetOrAddChunkFinal(ref c, valueData);
                    return;
                }
            }
            else
            {
                // Field is not in map - search for field.
                // Field might be exist, so we should search for non-removed
                // fields.
                // TODO: (Low) (ArzRecord) SetOrAddRaw: at this loop we can collect information about removed
                // fields onto some stack (it needs only two positions - one which 
                // exactly fits by space, and other with nearest neccessary space
                // available)... However i need more information about usefulness of this feature.
                _fdb0.GetCursor(out var c);
                while (!c.AtEnd)
                {
                    if (!c.Removed && c.NameId == nameId) // TODO: (ArzRecord) it is safe and should be more profitable to check name and then removed flag then?
                    {
                        SetOrAddChunkFinal(ref c, valueData);
                        return;
                    }

                    c.MoveNext();
                }
            }

            AppendChunk(valueData);
            Epilogue(true, false);
            return;

            void SetOrAddChunkFinal(ref ArzFieldCursor c, ReadOnlySpan<byte> valueData)
            {
                if (throwOnAdd) throw ArzError.FieldAlreadyExist(name);

                if (c.ValueCount == valueCount)
                {
                    // Found field, update it's type & value.
                    _fdb0.Update(c.Position, valueType, valueData);
                    // TODO: (Medium) (ArzRecord) Instead of ArzFieldDataBlock::Update/Append/etc methods it should be better to use ArzFieldCursor?
                    // E.g. ArzFieldDataBlock will do only chunk management, while ArzFieldCursor will setup data.
                    //c.FieldType = ArzValueType.Integer;
                    //c.RawValue = value;
                    Epilogue(false, false);
                    return;
                }
                else if (c.ValueCount < valueCount)
                {
                    // We need determine if this field is last field,
                    // because in that case we can just overwrite it
                    // with buffer expanding, without marking it as
                    // removed.
                    if (c.IsTailChunk)
                    {
                        _fdb0.UpdateTailChunk(c.Position, valueType, valueData);
                        // TODO: (Medium) (ArzRecord) Instead of ArzFieldDataBlock::UpdateTailChunk methods it should be better to use ArzFieldCursor?
                        // E.g. ArzFieldDataBlock will do only chunk management, while ArzFieldCursor will setup data.
                        Epilogue(false, false);
                        return;
                    }
                    else
                    {
                        // Field chunk has no space to hold our new
                        // value. Mark it as removed and append new
                        // chunk.
                        // TODO: (Medium) (ArzRecord) SetOrAddRaw: might try to search for new space, instead of appending to a new chunk. But i want get some stats for this feature...
                        c.SetRemoved();
                        AppendChunk(valueData);
                        Epilogue(false, true);
                        return;
                    }
                }
                else if (c.ValueCount > valueCount)
                {
                    if (c.IsTailChunk)
                    {
                        _fdb0.UpdateTailChunk(c.Position, valueType, valueData);
                        Epilogue(false, false);
                        return;
                    }
                    else
                    {
                        var requiredChunkSize = ArzFieldCursor.GetChunkSize(valueCount);
                        var ephemeralChunkSize = c.ChunkSize - requiredChunkSize;

                        if (ephemeralChunkSize >= ArzFieldCursor.MinimumChunkSize)
                        {
                            // Do shrinking:
                            // Write field over cursor, move next and setup next (empty) chunk with right size.
                            _fdb0.UpdateForSplit(c.Position, valueType, valueData);
                            c.MoveNext();
                            DebugCheck.True(!c.AtEnd);
                            _fdb0.UpdateEphemeral(c.Position, ephemeralChunkSize);
                            Epilogue(false, true);
                            return;
                        }
                        else
                        {
                            // Chunk can't be splitted.
                            // TODO: (Medium) (ArzRecord) SetOrAddRaw: might try to search for new space, instead of appending to a new chunk. But i want get some stats for this feature...
                            c.SetRemoved();
                            AppendChunk(valueData);
                            Epilogue(false, true);
                            return;
                        }
                    }
                }
                else throw Error.Unreachable();
            }

            void AppendChunk(ReadOnlySpan<byte> valueData)
            {
                // TODO: (Medium) (ArzRecord) ArzFieldDataBlock::Append may return cursor, so updates will go thru cursor.
                var fieldPosition = _fdb0.Append(nameId, valueType, valueData);
                if (fieldMap != null)
                {
                    fieldMap[nameId] = MakeFieldPtr(fieldPosition);
                }
            }

            void Epilogue(bool newFieldAdded, bool removedFieldChunk)
            {
                if (newFieldAdded && HasCount) _fieldCount++;
                if (removedFieldChunk) _numberOfRemovedFields++;

                _version++;
                _flags |= RecordFlags.DataModified;
            }
        }

        private bool RemoveImpl(string name)
        {
            LoadFieldDataIfNeed();
            if (!StringTable.TryGet(name, out var nameId)) return false;

            // TODO: (Low) (ArzField) Reorganize RemoveImpl.

            var updated = false;
            var found = false;

            var fieldMap = GetOrCreateFieldMap();
            if (fieldMap != null)
            {
                if (fieldMap.TryGetValue(nameId, out var fieldPtr))
                {
                    _fdb0.GetCursor(GetPositionFromFieldPtr(fieldPtr), out var c);
                    DebugCheck.True(c.NameId == nameId);

                    c.SetRemoved();
                    fieldMap.Remove(nameId);

                    found = true;
                    updated = true;
                }
                else
                {
                    // Field is not found in map.
                    return false;
                }
            }
            else
            {
                _fdb0.GetCursor(out var c);
                while (!c.AtEnd)
                {
                    if (!c.Removed && c.NameId == nameId)
                    {
                        c.SetRemoved();

                        updated = true;
                        found = true;
                        break;
                    }

                    c.MoveNext();
                }
            }

            if (!found)
            {
                return false;
            }

            if (updated)
            {
                if (HasCount) _fieldCount--;
                _numberOfRemovedFields++;
            }

            Check.True(updated);

            _version++;
            _flags |= RecordFlags.DataModified;
            return true;
        }

        private static arz_field_ptr MakeFieldPtr(int position)
        {
            return (arz_field_ptr)position;
        }

        private static int GetPositionFromFieldPtr(arz_field_ptr fieldPtr)
        {
            return (int)fieldPtr;
        }

        // TODO: (Low) (ArzRecord) might need stats for removed data / removed bytes
        // and may be support trimming / compressing of filed data block
        // (can be done over same buffer).

        #region Flags

        internal bool HasNoFieldData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.FieldDataMask) == RecordFlags.NoFieldData;
        }

        internal bool HasRawFieldData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.FieldDataMask) == RecordFlags.RawFieldData;
        }

        internal bool HasFieldData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.FieldDataMask) == RecordFlags.FieldData;
        }

        internal bool HasCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.HasCount) != 0;
        }

        internal bool IsModified
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.Modified) != 0;
        }

        internal bool IsDataModified
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.DataModified) != 0;
        }

        internal bool IsNew
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.New) != 0;
        }

        internal bool HasExplicitTimestamp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & RecordFlags.ExplicitTimestamp) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFieldDataState(RecordFlags flags)
        {
            DebugCheck.True((flags & RecordFlags.FieldDataMask) == flags);
            _flags = (_flags & ~RecordFlags.FieldDataMask) | flags;
        }

        [Flags]
        private enum RecordFlags
        {
            None = 0,

            // 3 bits are reserved for data store.
            // They are specify what is stored in FDB0 slot.
            NoFieldData = 0b000,
            RawFieldData = 0b001,
            // RawFieldData2 = 0b010,
            FieldData = 0b111,
            FieldDataMask = 0b111,

            HasCount = 1 << 3,

            Modified = 1 << 4,
            DataModified = 1 << 5,

            // New record is not backed by file, so we should not ask context for data.
            New = 1 << 6,

            // Has assigned (explicit) timestamp.
            // New records has no timestamp set, which will be assigned on write.
            ExplicitTimestamp = 1 << 7,
        }

        #endregion

        #region IRecordMetrics

        int IArzRecordMetrics.Version => _version;

        int IArzRecordMetrics.NumberOfRemovedFields => _numberOfRemovedFields;

        #endregion
    }
}
