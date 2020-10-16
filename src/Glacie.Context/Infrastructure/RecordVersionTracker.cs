using System;
using System.Collections.Generic;

using Glacie.Data.Arz;

namespace Glacie
{
    // TODO: VersionTracker bit ugly, because depend on internal order.
    // It probably should utilize gx_record_id to implement different selection
    // logic. Also trick with array is not great (however may be more efficient).

    [Obsolete("This need to validate records based on their version (changes), but this class done not nicely, and validation is external process.")]
    internal sealed class RecordVersionTracker
    {
        private readonly Database _database;
        private int[] _recordVersions;

        public RecordVersionTracker(Database database)
        {
            _database = database;
            _recordVersions = Array.Empty<int>();
        }

        public IEnumerable<ArzRecord> SelectChangedUnderlyingRecords()
        {
            var cachedValidatedRecordCount = _recordVersions.Length;
            int[] cachedValidatedRecordVersions;

            if (cachedValidatedRecordCount < _database.Count)
            {
                cachedValidatedRecordVersions = new int[_database.Count];
            }
            else
            {
                cachedValidatedRecordVersions = _recordVersions;
            }

            var recordIndex = 0;
            foreach (var record in _database.SelectAll())
            {
                var underlyingRecord = record.GetUnderlyingRecord(writing: false);
                var recordVersion = underlyingRecord.Version;

                int validatedRecordVersion;
                if (recordIndex < cachedValidatedRecordCount)
                {
                    validatedRecordVersion = cachedValidatedRecordVersions[recordIndex];
                }
                else
                {
                    validatedRecordVersion = recordVersion - 1;
                }

                if (recordVersion != validatedRecordVersion)
                {
                    cachedValidatedRecordVersions[recordIndex] = recordVersion;

                    yield return underlyingRecord;
                }

                recordIndex++;
            }

            Check.That(recordIndex == cachedValidatedRecordVersions.Length);

            _recordVersions = cachedValidatedRecordVersions;
        }

        public IEnumerable<ArzRecord> SelectAllUnderlyingRecords()
        {
            var recordVersions = _recordVersions;
            if (_database.Count != _recordVersions.Length)
            {
                recordVersions = new int[_database.Count];
            }

            var recordIndex = 0;
            foreach (var record in _database.SelectAll())
            {
                var underlyingRecord = record.GetUnderlyingRecord(writing: false);
                recordVersions[recordIndex] = underlyingRecord.Version;

                yield return underlyingRecord;
            }

            _recordVersions = recordVersions;
        }
    }
}
