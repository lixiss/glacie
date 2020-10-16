using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Abstractions
{
    public abstract class RecordResolver
        : IRecordResolver
        , IDisposable
    {
        protected RecordResolver() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        public abstract IEnumerable<Record> SelectAll();

        public abstract Resolution<Record> ResolveRecord(Path path);

        public virtual bool TryResolveRecord(Path path, [NotNullWhen(returnValue: true)] out Record result)
        {
            var resolution = ResolveRecord(path);
            if (resolution.HasValue)
            {
                result = resolution.Value;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}
