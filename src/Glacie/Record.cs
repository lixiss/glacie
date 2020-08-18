using System;
using System.Runtime.CompilerServices;
using Glacie.Abstractions;
using Glacie.Data.Arz;
using Glacie.Infrastructure;

namespace Glacie
{
    // Record should have stable identity.
    // To achieve this, they may be done as reference types, or
    // as struct with access record by id.

    public readonly struct Record // : IRecordApi<Field, Field?, Field>
    {
        private readonly Database _database;
        private readonly gx_record_id _id;

        internal Record(Database database, gx_record_id id)
        {
            _database = database;
            _id = id;
        }

        // TODO: Add equivalent to ArzRecord?
        public readonly Database Database => _database;
        public readonly Context Context => _database.Context;

        // TODO: Instead of exposing fields via records, we might expose
        // access to underlying record which already has everything we need.
        // However this is bit ugly API.
        [Obsolete("Experimental. Undecided. This API call might be not available in future versions.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArzRecord GetUnderlyingRecord(bool write = false)
        {
            if (write) return GetRecordForTarget();
            else return GetRecord();
        }


        // TODO: Use properties, alike RecordForReading, RecordForWriting...
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ArzRecord GetRecord() => _database.GetRecord(_id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ArzRecord GetRecordForTarget() => _database.GetRecordForTarget(_id);


        // TODO: (Gx) Record Name Normalization: This might behave incorrectly, because
        // record name in ARZ database might be not normalized.
        public readonly string Name => GetRecord().Name;

        public string Class
        {
            readonly get => GetRecord().Class;
            set => GetRecordForTarget().Class = value;
        }

        public Variant this[string fieldName]
        {
            readonly get => GetRecord()[fieldName];
            set => GetRecordForTarget()[fieldName] = value;
        }
    }
}
