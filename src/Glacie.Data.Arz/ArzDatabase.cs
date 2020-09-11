using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Glacie.Abstractions;
using Glacie.Data.Arz.Infrastructure;
using IO = System.IO;

namespace Glacie.Data.Arz
{
    using RecordMap = Dictionary<arz_string_id, ArzRecord>;
    // TODO: (Medium) (ArzDatabase) (Decision) ArzDatabase is collection of ArzRecord, so we can implement IReadOnlyCollection<ArzRecord> or IReadOnlyList<ArzRecord> interfaces.
    // However, i'm not sure what will like this. Generally i'm doesn't like to regular LINQ will be applied.
    // ArzDatabase provide GetAll method which easy and understandable.

    // TODO: (Medium) (ArzDatabase) Naming of API. GetAll method might be better to SelectAll?

    public sealed class ArzDatabase : IArzDatabase,
        IDisposable,
        IDatabaseApi<ArzRecord, ArzRecord?, ArzRecord?>
    {
        #region Factory

        public static ArzDatabase Create() => ArzMemoryContext.Create();

        public static ArzDatabase Open(string path, ArzReaderOptions? options = null)
            => ArzReader.Read(path, options);

        public static ArzDatabase Open(IO.Stream stream, ArzReaderOptions? options = null)
            => ArzReader.Read(stream, options);

        #endregion

        private readonly ArzContext _context;

        private readonly List<ArzRecord> _records;
        private RecordMap? _recordMap;

        internal ArzDatabase(ArzContext context, List<ArzRecord> records)
        {
            _context = context;
            _records = records;
        }

#if DEBUG
        ~ArzDatabase()
        {
            throw Error.InvalidOperation("{0} must be explicitly disposed.", nameof(ArzDatabase));
        }
#endif

        public void Dispose()
        {
            _context?.Dispose();
            GC.SuppressFinalize(this);
        }

        public IArzContext GetContext() => _context;

        #region API

        public int Count => _records.Count;

        public IEnumerable<ArzRecord> GetAll()
            => _records; // TODO: (ArzDatabase) This might be not optimal, return struct version like ArzStringTable.

        // TODO: (Medium) (ArzDatabase) Select by record Class. + set of classes.
        // TODO: (Medium) (ArzDatabase) Select by record Name by glob or regex.

        public ArzRecord Get(string name)
            => GetOrNull(name) ?? throw ArzError.RecordNotFound(name);

        public bool TryGet(string name, [NotNullWhen(returnValue: true)] out ArzRecord? record)
        {
            record = GetOrNull(name);
            return record != null;
        }

        public ArzRecord? GetOrNull(string name)
        {
            if (StringTable.TryGet(name, out var nameId))
            {
                var recordMap = GetOrCreateRecordMap();
                if (recordMap != null)
                {
                    if (recordMap.TryGetValue(nameId, out var result))
                    {
                        return result;
                    }
                    else return null;
                }
                else
                {
                    foreach (var record in _records)
                    {
                        if (record.NameId == nameId) return record;
                    }
                }
            }
            return null;
        }

        public ArzRecord this[string name]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Get(name);
        }

        public ArzRecord Add(string name, string? @class = null)
        {
            if (TryGet(name, out var _))
            {
                throw ArzError.RecordAlreadyExist(name);
            }
            return AddCore(name, @class);
        }

        public ArzRecord GetOrAdd(string name, string? @class = null)
        {
            if (TryGet(name, out var record))
            {
                // TODO: (High) (ArzDatabase) How this API should behave, when we call GetOrAdd, but already existing record has different class? Is this error?
                // When @class == null -> we can don't care (like now)
                // When @class specified we might care.
                return record;
            }
            return AddCore(name, @class);
        }

        private ArzRecord AddCore(string name, string? @class)
        {
            var nameId = StringTable.GetOrAdd(name);
            var classId = RecordClassTable.GetOrAdd(@class);
            var record = new ArzRecord(_context, nameId, classId);
            _records.Add(record);

            var recordMap = GetRecordMap();
            if (recordMap != null)
            {
                recordMap.Add(nameId, record);
            }

            return record;
        }

        public bool Remove(string name)
        {
            if (TryGet(name, out var record))
            {
                return Remove(record);
            }
            return false;
        }

        public bool Remove(ArzRecord record)
        {
            if (record.Context != (object)_context)
            {
                throw ArzError.ExternalRecord();
            }

            // TODO: (Medium) (ArzDatabase) Instead of removing we might just mark it as removed or put null on list.
            // This should be more important for Adopt-calls.
            var removed = _records.Remove(record);

            var recordMap = GetRecordMap();
            if (recordMap != null)
            {
                var removedFromMap = recordMap.Remove(record.NameId);
                Check.True(removed == removedFromMap);
            }

            return removed;
        }

        public void Import(ArzDatabase database)
        {
            Check.Argument.NotNull(database, nameof(database));
            if (database == this) throw Error.Argument(nameof(database));

            var stringEncoder = new ArzStringEncoder(database.StringTable, StringTable);
            foreach (var record in database.GetAll())
            {
                ImportInternal(record, stringEncoder);
            }
        }

        public ArzRecord Import(ArzRecord record)
        {
            Check.Argument.NotNull(record, nameof(record));
            if (record.Context == (object)_context)
            {
                throw ArzError.InternalRecord();
            }

            var stringEncoder = new ArzStringEncoder(record.StringTable, StringTable);
            return ImportInternal(record, stringEncoder);
        }

        public ArzRecord Import(ArzRecord record, ArzStringEncoder stringEncoder)
        {
            Check.Argument.NotNull(record, nameof(record));
            Check.Argument.NotNull(stringEncoder, nameof(stringEncoder));

            if (record.Context == (object)_context)
            {
                throw ArzError.InternalRecord();
            }

            if (stringEncoder.SourceStringTable != record.StringTable)
            {
                throw Error.Argument(nameof(stringEncoder), "Given string encoder doesn't belong to record.");
            }

            if (stringEncoder.TargetStringTable != StringTable)
            {
                throw Error.Argument(nameof(stringEncoder), "Given string encoder doesn't belong to this database.");
            }

            return ImportInternal(record, stringEncoder);
        }

        private ArzRecord ImportInternal(ArzRecord record, ArzStringEncoder stringEncoder)
        {
            if (TryGet(record.Name, out var internalRecord))
            {
                internalRecord.Class = record.Class;
            }
            else
            {
                internalRecord = Add(record.Name, record.Class);
            }
            internalRecord.ImportFrom(record, stringEncoder);
            return internalRecord;
        }

        [Obsolete("Not yet implemented.", true)]
        public void Adopt(ArzDatabase database)
        {
            // TODO: Adopt(ArzDatabase)
            throw Error.NotImplemented();
        }

        [Obsolete("Not yet implemented.", true)]
        public ArzRecord Adopt(ArzRecord record)
        {
            // TODO: Adopt(ArzRecord)
            throw Error.NotImplemented();
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private RecordMap? GetRecordMap()
        {
            if (Features.RecordMapEnabled)
            {
                return _recordMap;
            }
            else return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private RecordMap? GetOrCreateRecordMap()
        {
            if (Features.RecordMapEnabled)
            {
                return GetRecordMap() ?? MaybeCreateRecordMap();
            }
            else return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RecordMap? MaybeCreateRecordMap()
        {
            // TODO: (Low) (ArzDatabase) RecordMap creation heuristics might need.
            var recordMap = CreateRecordMap();

            _recordMap = recordMap;

            return recordMap;
        }

        private RecordMap CreateRecordMap()
        {
            var recordMap = new RecordMap(_records.Count);
            foreach (var record in _records)
            {
                recordMap.Add(record.NameId, record);
            }
            return recordMap;
        }

        #region Context

        internal ArzContext Context => _context;

        private ArzStringTable StringTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _context.StringTable;
        }

        private ArzRecordClassTable RecordClassTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _context.RecordClassTable;
        }

        #endregion
    }
}
