using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Glacie.Abstractions;
using Glacie.Data.Arz;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Infrastructure;

namespace Glacie
{
    using RecordMapByNameId = Dictionary<arz_string_id, gx_record_id>;
    using RecordMapByName = Dictionary<string, gx_record_id>;

    public sealed partial class Database // TODO: (Gx) : IDatabaseApi<Record, Record?, Record>
    {
        private readonly Context _context;
        private readonly ArzDatabase _database;

        private RecordSlotTable _recordTable;
        private RecordMapByName _recordMap;

        internal Database(Context context, ArzDatabase database)
        {
            Check.Argument.NotNull(context, nameof(context));
            Check.Argument.NotNull(database, nameof(database));

            _context = context;
            _database = database;

            _recordTable = new RecordSlotTable(capacity: Math.Max(_database.Count, 1024));
            _recordMap = new RecordMapByName(StringComparer.Ordinal);
        }

        internal void LinkRecordReferences(ArzDatabase database)
        {
            var isTarget = _database == database;

            var linkedRecordCount = 0;
            foreach (var record in database.GetAll())
            {
                var recordName = record.Name;
                // TODO: (Gx) Record Name Normalization
                if (!_recordMap.TryGetValue(recordName, out var _))
                {
                    var recordId = _recordTable.Add(new RecordSlot(record, isTarget));
                    _recordMap.Add(recordName, recordId);
                    linkedRecordCount++;
                }
            }

            // TODO: (Gx) Need Logging
            Console.WriteLine("        # of Records: {0}", database.Count);
            Console.WriteLine(" # of Records Linked: {0}", linkedRecordCount);
        }

        #region Import and Adopt

        [Obsolete("Not yet implemented.", true)]
        public void Import(Record record)
        {
            // TODO: (Gx) Database::Import(Record) not implemented.
            throw Error.NotImplemented();
        }

        [Obsolete("Not yet implemented.", true)]
        public void Adopt(Record record)
        {
            // TODO: (Gx) Database::Adopt(Record) not implemented. This method may be not needed.
            throw Error.NotImplemented();
        }

        [Obsolete("Not yet implemented.", true)]
        public void Import(IArzDatabase database)
        {
            // TODO: (Gx) Database::Import(IArzDatabase) not implemented.
            Check.Argument.NotNull(database, nameof(database));
            throw Error.NotImplemented();
            // _database.Import(externalDatabase);
        }

        [Obsolete("Not yet implemented.", true)]
        public void Import(IArzRecord record)
        {
            // TODO: (Gx) Database::Import(IArzDatabase) not implemented.
            Check.Argument.NotNull(record, nameof(record));
            throw Error.NotImplemented();
        }

        [Obsolete("Not yet implemented.", true)]
        public void Adopt(IArzDatabase database)
        {
            // TODO: (Gx) Database::Import(IArzDatabase) not implemented.
            Check.Argument.NotNull(database, nameof(database));
            if (_database == (object)database) throw Error.InvalidOperation("You can't adopt same object.");
            throw Error.NotImplemented();
        }

        [Obsolete("Not yet implemented.", true)]
        public void Adopt(IArzRecord record)
        {
            // TODO: (Gx) Database::Import(IArzDatabase) not implemented.
            Check.Argument.NotNull(record, nameof(record));
            throw Error.NotImplemented();
        }

        #endregion

        #region API

        public Context Context => _context;

        public int Count => _recordTable.Count;

        public IEnumerable<Record> GetAll()
        {
            foreach (var recordId in _recordMap.Values)
            {
                yield return new Record(this, recordId);
            }
        }

        public Record Get(string name)
        {
            if (_recordMap.TryGetValue(name, out var recordId))
            {
                return new Record(this, recordId);
            }
            else throw GxError.RecordNotFound(name);
        }

        public bool TryGet(string name, out Record record)
        {
            if (_recordMap.TryGetValue(name, out var recordId))
            {
                record = new Record(this, recordId);
                return true;
            }
            else
            {
                record = default;
                return false;
            }
        }

        public Record? GetOrNull(string name)
        {
            if (_recordMap.TryGetValue(name, out var recordId))
            {
                return new Record(this, recordId);
            }
            else return null;
        }

        public Record this[string name]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Get(name);
        }

        #endregion

        #region Internal API

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArzRecord GetRecord(gx_record_id id)
        {
            return _recordTable[id].Record;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArzRecord GetRecordForTarget(gx_record_id id)
        {
            ref var slot = ref _recordTable[id];
            if (slot.IsTarget) return slot.Record;
            else return GetRecordForTargetSlow(ref slot);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ArzRecord GetRecordForTargetSlow(ref RecordSlot recordSlot)
        {
            DebugCheck.True(!recordSlot.IsTarget);

            // TODO: (Gx) (Performance) In order if sources will not be consumed directly, we may use Adopt instead of Import.
            // TODO: (Gx) Use cached string encoder.
            var record = _database.Import(recordSlot.Record);
            recordSlot = new RecordSlot(record, true);
            return record;
        }

        #endregion
    }
}
