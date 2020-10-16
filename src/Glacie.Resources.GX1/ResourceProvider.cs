using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Resources
{
    public abstract class ResourceProvider
        : IResourceProvider
        , IDisposable
    {
        protected ResourceProvider() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        public abstract IEnumerable<Resource> SelectAll();
        public abstract bool TryGetResource(Path path, [NotNullWhen(true)] out Resource? result);

        public virtual Resource GetResource(Path path)
        {
            if (TryGetResource(path, out var result)) return result;
            else throw Error.InvalidOperation("Unable to get resource: \"{0}\".", path);
        }

        public abstract bool Exists(Path path);
    }
}
