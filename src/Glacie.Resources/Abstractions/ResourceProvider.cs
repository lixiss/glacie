using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using IO = System.IO;

namespace Glacie.Abstractions
{
    public abstract class ResourceProvider : IResourceProvider, IDisposable
    {
        private bool _disposed;
        private readonly ResourceType[]? _supportedResourceTypes;

        protected ResourceProvider(ResourceType[]? supportedResourceTypes)
        {
            _supportedResourceTypes = supportedResourceTypes;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
        }

        protected bool Disposed => _disposed;

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw Error.ObjectDisposed(GetType().ToString());
        }

        public IReadOnlyList<ResourceType> GetSupportedResourceTypes()
            => _supportedResourceTypes ?? Array.Empty<ResourceType>();

        public bool IsResourceTypeSupported(ResourceType value)
        {
            if (_supportedResourceTypes == null
                || _supportedResourceTypes.Length == 0) return true;

            for (var i = 0; i < _supportedResourceTypes.Length; i++)
            {
                if (_supportedResourceTypes[i] == value) return true;
            }

            return false;
        }

        public abstract IEnumerable<IResource> SelectAll();

        public abstract bool TryGet(in VirtualPath name,
            [NotNullWhen(returnValue: true)] out IResource? result);

        public virtual IResource? GetOrDefault(in VirtualPath name)
        {
            if (TryGet(in name, out var result)) return result;
            return default;
        }

        public virtual IResource Get(in VirtualPath name)
        {
            if (TryGet(in name, out var result)) return result;
            else
            {
                throw Error.InvalidOperation("Resource not found: \"{0}\".", name.ToString()); // TODO: ResourceNotFound exception
            }
        }

        public virtual IO.Stream Open(in VirtualPath name)
        {
            ThrowIfDisposed();

            return Get(name).Open();
        }

        public abstract IO.Stream Open(IResource resource);
    }
}
