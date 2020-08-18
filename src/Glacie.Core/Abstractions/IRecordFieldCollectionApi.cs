using System.Collections.Generic;

namespace Glacie.Abstractions
{
    // TODO: (Low) Rename to IFieldCollectionApiContract.
    // TODO: (Low) Include this interface only as conditional compile-time feature.

    internal interface IRecordFieldCollectionApi<TField, TFieldOrNull, TFieldOutOrNull>
    {
        IEnumerable<TField> GetAll();
        TField Get(string name);
        TFieldOrNull GetOrNull(string name);
        bool TryGet(string name, out TFieldOutOrNull value);
        bool Remove(TField field);
    }
}
