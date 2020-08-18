using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Abstractions
{
    // TRecord - represents record's type
    // TRecordOrNull - represents nullable record's type
    // TRecordOutOrNull - same as TRecordOrNull, but when TRecord is:
    //   - reference type -> it effectively might be `TRecord?`
    //   - value type - then it should be `TRecord` (assuming default value is represents empty/null value itself).

    // TODO: (Low) Rename to IDatabaseApiContract.
    // TODO: (Low) Include this interface only as conditional compile-time feature.
    internal interface IDatabaseApi<TRecord, TRecordOrNull, TRecordOutOrNull>
    {
        int Count { get; }

        IEnumerable<TRecord> GetAll();

        TRecord Get(string name);

        bool TryGet(string name, [NotNullWhen(returnValue: true)] out TRecordOutOrNull record);

        TRecordOrNull GetOrNull(string name);

        TRecord this[string name] { get; }

        TRecord Add(string name, string? @class = null);

        TRecord GetOrAdd(string name, string? @class = null);

        bool Remove(string name);

        bool Remove(TRecord record);
    }
}
