using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Abstractions
{
    public interface IRecordResolver
    {
        IEnumerable<Record> SelectAll();

        Resolution<Record> ResolveRecord(Path path);
        bool TryResolveRecord(Path path, [NotNullWhen(true)] out Record record);
    }
}
