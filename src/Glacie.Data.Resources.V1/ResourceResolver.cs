using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Glacie.Abstractions;

namespace Glacie.Data.Resources.V1
{
    // TODO: ...
    internal sealed class ResourceResolver : IResourceResolver, IResourceCollection
    {
        private readonly Dictionary<string, Resource> _resources;
        private readonly Path1Form _virtualPathForm;

        public ResourceResolver(Path1Form virtualPathForm)
        {
            _resources = new Dictionary<string, Resource>(StringComparer.Ordinal);
            _virtualPathForm = virtualPathForm;
        }

        #region IResourceResolver

        public bool TryResolve(in Path1 path, [NotNullWhen(true)] out Resource? result)
        {
            var key = path.ToForm(_virtualPathForm);
            return _resources.TryGetValue(key.ToString(), out result);
        }

        public Resource? ResolveOrDefault(in Path1 path)
        {
            if (TryResolve(in path, out var result)) return result;
            else return null;
        }

        public Resource Resolve(in Path1 path)
        {
            if (TryResolve(in path, out var result)) return result;
            else
            {
                // TODO: use proper exception text or diagnostics
                throw Error.InvalidOperation("Unable to resolve resource \"{0}\".", path.ToString());
            }
        }

        public bool TryResolve(string path, [NotNullWhen(returnValue: true)] out Resource? result)
            => TryResolve(Path1.From(path), out result);

        public Resource? ResolveOrDefault(string path) => ResolveOrDefault(Path1.From(path));

        public Resource Resolve(string path) => Resolve(Path1.From(path));

        #endregion

        #region IResourceCollection

        public IReadOnlyCollection<Resource> SelectAll()
        {
            return _resources.Values;
        }

        #endregion
    }
}
