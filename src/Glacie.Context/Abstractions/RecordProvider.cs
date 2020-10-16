using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Abstractions
{
    public abstract class RecordProvider
        : IRecordProvider
        , IDisposable
    {
        protected RecordProvider() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        public abstract IEnumerable<Record> SelectAll();
        public abstract bool TryGetRecord(Path path, [NotNullWhen(true)] out Record result);

        public virtual Record GetRecord(Path path)
        {
            if (TryGetRecord(path, out var result)) return result;
            else throw GxError.RecordNotFound(path.ToString());
        }
    }
}
