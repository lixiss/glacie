using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;

namespace Glacie.Resources
{
    public abstract class ResourceResolver
        : IResourceResolver
        , IDisposable
    {
        protected ResourceResolver() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        public abstract IEnumerable<Resource> SelectAll();

        public abstract Resolution<Resource> ResolveResource(Path path);

        public virtual bool TryResolveResource(Path path, [NotNullWhen(returnValue: true)] out Resource? result)
        {
            var resolution = ResolveResource(path);
            if (resolution.HasValue)
            {
                result = resolution.Value;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}
