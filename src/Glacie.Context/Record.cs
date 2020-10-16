using System;
using System.Runtime.CompilerServices;
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

        public readonly Database Database => _database;
        public readonly Context Context => _database.Context;

        // TODO: Implement read-only checks in ArzDatabase.
        // So at least it will help with detection of API misuse. Check should we cheap,
        // because record can have own "ReadOnly" flag, which will be flowed from database/context.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ArzRecord GetUnderlyingRecord() => RecordForReading;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArzRecord GetUnderlyingRecord(bool writing)
        {
            if (writing) return RecordForWriting;
            else return RecordForReading;
        }

        private readonly ArzRecord RecordForReading
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _database.GetRecordForReading(_id);
        }

        private ArzRecord RecordForWriting
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _database.GetRecordForWriting(_id);
        }

        // TODO: (Gx) Record Name Normalization: This might behave incorrectly, because
        // record name in ARZ database might be not normalized. We want report
        // both Name (in Context.Database) from record slot, and Path.
        // They might be used later for "record name" restoring (a original casing).
        public readonly string Name => RecordForReading.Name;
        public readonly Path Path => new Path(Name);

        public string Class
        {
            readonly get => RecordForReading.Class;
            set => RecordForWriting.Class = value;
        }

        public int Version => RecordForReading.Version;

        public Variant this[string fieldName]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => RecordForReading[fieldName];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => RecordForWriting[fieldName] = value;
        }
    }
}
