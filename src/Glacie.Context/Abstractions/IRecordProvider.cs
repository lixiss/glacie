using System;
using System.Collections.Generic;
using System.Text;

namespace Glacie.Abstractions
{
    public interface IRecordProvider
    {
        IEnumerable<Record> SelectAll();

        bool TryGetRecord(Path path, out Record record);
        Record GetRecord(Path path);
    }
}
