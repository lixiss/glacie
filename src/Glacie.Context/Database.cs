using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Glacie.Abstractions;
using Glacie.Data.Arz;
using Glacie.Infrastructure;
using Glacie.Targeting.Infrastructure;

namespace Glacie
{
    // TODO: Add caseSensitive option.
    // TODO: Add output path form / mapper driven by EngineType?
    // TODO: Get prefered path form source (to be able validate it).




    // Database mixes multiple ArzDatabase and enables uniform access to them.
    // Records with same names are "shadowed" by most recent database
    // (target database included).
    //
    // Record names generally is paths in virtual file system, so database
    // handle them in path-like manner, and normally perform path normalization,
    // and gives access in directory-separator insensitive and case-insensitive
    // manner (if configured by defaults). Both TQxx and GD use names in lower
    // case, however TQxx uses backslash directory separator (\), while GD
    // uses forward slash (/).
    //
    // When database peforms source record linking, it is done by converting all
    // input record names according to defined path form, so even if input
    // record names was in different forms, they should be correctly linked
    // anyway (according to path conversion rules).
    //
    // However, underlying (source or target) database, may use record names in
    // another form. (For example output with different directory separator.)
    //
    // Regardless to this, any databases should have valid path names.
    // (E.g. has only valid path characters).
    //
    // However database should able to load records in any form, but when new
    // records added into database, they should be strictly valid (to block
    // errors early.)
    //
    // Also database may create new or import existing records in output
    // database in their path form. Which may be different, from from internal
    // path form used in database to establish record identity.

    // using RecordMapByNameId = Dictionary<arz_string_id, gx_record_id>;
    // TODO: there is possible to create record map by StringSymbol, but doesn't know if it will be faster.
    using RecordMapByName = Dictionary<string, gx_record_id>;

    public sealed partial class Database : RecordProvider // TODO: (Gx) : IDatabaseApi<Record, Record?, Record>
    {
        private readonly Context _context;
        private readonly ArzDatabase _database;
        private readonly DatabaseConventions _conventions;

        private RecordSlotTable _recordTable;
        private RecordMapByName _recordMap;

        // TODO: use Logger<Database>
        internal Database(Context context, ArzDatabase database, DatabaseConventions conventions)
        {
            Check.Argument.NotNull(context, nameof(context));
            Check.Argument.NotNull(database, nameof(database));
            Check.Argument.NotNull(conventions, nameof(conventions));

            _context = context;
            _database = database;
            _conventions = conventions;

            _recordTable = new RecordSlotTable(capacity: Math.Max(_database.Count, 1024));
            _recordMap = new RecordMapByName(GetPathComparer(_conventions.IsCaseSensitive));
        }

        internal void LinkRecordReferences(ArzDatabase database)
        {
            var isTarget = _database == database;

            var linkedRecordCount = 0;
            foreach (var record in database.SelectAll())
            {
                var recordName = record.Name;
                var recordPath = Path.Implicit(recordName);
                if (!_recordMap.TryGetValue(recordPath.ToString(), out var _))
                {
                    var recordId = _recordTable.Add(new RecordSlot(record, isTarget));
                    _recordMap.Add(recordPath.ToString(), recordId);
                    linkedRecordCount++;
                }
            }

            Context.Log.Debug("        # of Records: {0}", database.Count);
            Context.Log.Debug(" # of Records Linked: {0}", linkedRecordCount);
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

        public override IEnumerable<Record> SelectAll()
        {
            foreach (var recordId in _recordMap.Values)
            {
                yield return new Record(this, recordId);
            }
        }

        public Record GetRecord(string name)
        {
            // var recordPath = _recordNameMapper.Map(Path1.From(name));
            if (_recordMap.TryGetValue(name, out var recordId))
            {
                return new Record(this, recordId);
            }
            else throw GxError.RecordNotFound(name);
        }

        public override bool TryGetRecord(Path path, [NotNullWhen(true)] out Record result)
            => TryGetRecord(path.ToString(), out result);

        public bool TryGetRecord(string name, out Record result)
        {
            //if (!_recordNameMapper.TryMap(Path1.From(name), out var recordPath))
            //{
            //    record = default;
            //    return false;
            //}

            if (_recordMap.TryGetValue(name, out var recordId))
            {
                result = new Record(this, recordId);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public Record? GetOrNull(string name)
        {
            //if (!_recordNameMapper.TryMap(Path1.From(name), out var recordPath))
            //{
            //    return null;
            //}

            if (_recordMap.TryGetValue(name, out var recordId))
            {
                return new Record(this, recordId);
            }
            else return null;
        }

        public Record this[string name]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetRecord(name);
        }

        // TODO: Adding records should match conventions, even when dynamically
        // linking records, e.g. when adding.

        #endregion

        #region Internal API

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArzRecord GetRecordForReading(gx_record_id id)
        {
            return _recordTable[id].Record;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArzRecord GetRecordForWriting(gx_record_id id)
        {
            ref var slot = ref _recordTable[id];
            if (slot.IsTarget) return slot.Record;
            else return GetRecordForWritingSlow(ref slot);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ArzRecord GetRecordForWritingSlow(ref RecordSlot recordSlot)
        {
            DebugCheck.True(!recordSlot.IsTarget);

            // TODO: (Gx) (Performance) In order if sources will not be consumed directly, we may use Adopt instead of Import.
            // TODO: (Gx) Use cached string encoder.
            var record = _database.Import(recordSlot.Record);
            recordSlot = new RecordSlot(record, true);
            return record;
        }

        #endregion

        #region Helpers

        private static IEqualityComparer<string> GetPathComparer(bool isCaseSensitive)
        {
            if (isCaseSensitive) return PathComparer.Ordinal;
            else return PathComparer.OrdinalIgnoreCase;
        }

        #endregion
    }
}
