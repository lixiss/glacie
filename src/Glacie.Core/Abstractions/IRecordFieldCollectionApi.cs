using System.Collections.Generic;

using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    // TODO: (Low) Rename to IFieldCollectionApiContract.
    // TODO: (Low) Include this interface only as conditional compile-time feature.

    internal interface IRecordFieldCollectionApi<TField, TFieldOrNull, TFieldOutOrNull>
    {
        IEnumerable<TField> SelectAll();

        bool TryGet(string name, out TFieldOutOrNull value);
        bool TryGet(string name, ArzRecordOptions options, out TFieldOutOrNull value);

        bool Remove(string name);
        bool Remove(TField field);

        TField Get(string name);
        TField Get(string name, ArzRecordOptions options);

        TFieldOrNull GetOrNull(string name);
        TFieldOrNull GetOrNull(string name, ArzRecordOptions options);
    }
}
