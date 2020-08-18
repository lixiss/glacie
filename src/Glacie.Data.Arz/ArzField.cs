using System;
using System.Runtime.CompilerServices;
using Glacie.Abstractions;
using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz
{
    // TODO: (High) (ArzField) Try to implement version checks feature. Make it optional and compare performance.

    public readonly struct ArzField : IFieldApi
    {
        private readonly ArzRecord _record;
        private readonly arz_field_ptr _fieldPtr;
        // private readonly int _version;

        internal ArzField(ArzRecord record, arz_field_ptr fieldPtr)
        {
            _record = record;
            _fieldPtr = fieldPtr;
        }

        public string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _record.GetFieldName(_fieldPtr);
        }

        internal arz_string_id NameId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _record.GetFieldNameId(_fieldPtr);
        }

        public ArzValueType ValueType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _record.GetFieldValueType(_fieldPtr);
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _record.GetFieldValueCount(_fieldPtr);
        }

        // TODO: (ArzField) IsArray might be removed.
        [Obsolete("Undecided and might be removed in next versions.")]
        public bool IsArray => Count > 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>() => _record.GetFieldValue<T>(_fieldPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(int index) => _record.GetFieldValue<T>(_fieldPtr, index);

        // TODO: (ArzField) May be need a Get<>(index) which will be limited by actual array length,
        // so might be useful for reading difficulty triples / or 3x6 values.
        // However may be better to do this at higher level only?

        // TODO: (High) (ArzField) Complete API. Unify with Variant.
        // TODO: (High) (ArzField) Value iteration.

        #region Mutating

        // TODO: (High) (ArzField) ArzField::Set currently is not implemented.
        // Setting field's value may cause it's position be changed, however, same may occur if we change field's value 
        // via record's API. Field just not have own identity, so it might break if not properly used.
        // However this API allow doesn't do additional field lookups, so it is good idea to re-introduce this API back.
        // However still be a good idea to somehow protect self from errors.
        // TODO: As idea - we might move to use field-id instead which may be backed by fieldmap, => this will
        // give even more slower access when is not-mapped, but may provide stable API. (But then mutation will not able
        // split removed fields into field + removed). Not sure what it is also worth.
        // TODO: (!) Seems as good idea => check field's acessor by version. Requires version check.

        [Obsolete("Not yet implemented.", true)]
        public void Set(int value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(float value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(double value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(bool value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(string value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(int[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(float[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(double[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(bool[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(string[] value) => throw Error.NotImplemented();

        [Obsolete("Not yet implemented.", true)]
        public void Set(Variant value) => throw Error.NotImplemented();

        #endregion
    }
}
