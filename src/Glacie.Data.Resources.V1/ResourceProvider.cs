using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Glacie.Data.Resources.V1
{
    // TODO: (High) (ResourceProviders) Simplify ResourceProviders by removing VirtualBasePath.
    // They just provide access to resources, by simple IResource.Name
    // However, internalBasePath might be still useful, but remove logic as much
    // as possible. Resolvers will wrap IResource when needed, but not need
    // to store IResource, because it will be enough to access to resource via
    // it's name inside provider.


    /// <summary>
    /// Provides resources.
    /// </summary>
    /// <remarks>
    /// Client code most of time want to use <see cref="IResourceResolver" />.
    /// Current implementation of providers doesn't maintain resource identity
    /// (e.g. it always return new <see cref="Resource"/> instances.
    /// </remarks>
    public abstract class ResourceProvider
        : IDisposable
        , IResourceProvider
    {
        protected const Path1Form MinimumVirtualPathForm = Path1Form.Strict | Path1Form.Normalized;
        protected const Path1Form MinimumInternalPathForm = Path1Form.Strict | Path1Form.Normalized;

        private bool _disposed;
        private readonly string? _name;
        private readonly Path1 _virtualBasePath;
        private readonly Path1Form _virtualPathForm;
        private readonly Path1 _internalBasePath;
        private readonly Path1Form _internalPathForm;
        private readonly ResourceType[]? _supportedTypes;

        protected ResourceProvider(
            string? name,
            in Path1 virtualBasePath,
            Path1Form virtualPathForm,
            in Path1 internalBasePath,
            Path1Form internalPathForm,
            IEnumerable<ResourceType>? supportedTypes)
        {
            _name = name;

            if (virtualPathForm != Path1Form.Any)
            {
                throw Error.InvalidOperation("This parameter is obsoleted and should be default value.");
            }

            if (!virtualBasePath.IsEmpty)
            {
                throw Error.InvalidOperation("This parameter is obsoleted and should be default value.");
            }


            virtualPathForm |= MinimumVirtualPathForm;
            _virtualBasePath = virtualBasePath.ToFormNonEmpty(virtualPathForm);
            _virtualPathForm = virtualPathForm;

            // InternalBasePath used only to filter-out sub-resources.
            internalPathForm |= MinimumInternalPathForm;
            _internalBasePath = internalBasePath.ToFormNonEmpty(internalPathForm);
            _internalPathForm = internalPathForm;

            var supportedTypesArray = supportedTypes != null
                ? supportedTypes.Distinct().ToArray()
                : null;

            _supportedTypes = supportedTypesArray != null && supportedTypesArray.Length > 0
                ? supportedTypesArray
                : null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        protected bool Disposed => _disposed;

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw Error.ObjectDisposed(GetType().ToString());
        }

        public string? Name => _name;

        public ref readonly Path1 VirtualBasePath => ref _virtualBasePath;

        public Path1Form VirtualPathForm => _virtualPathForm;

        public ref readonly Path1 InternalBasePath => ref _internalBasePath;

        public Path1Form InternalPathForm => _internalPathForm;

        public Resource GetByPhysicalPath(string physicalPath)
        {
            if (TryGetByPhysicalPath(physicalPath, out var result)) return result;
            else
            {
                throw Error.InvalidOperation("Resource not found: \"{0}\".", physicalPath.ToString()); // TODO: ResourceNotFound exception
            }
        }

        public Resource? GetByPhysicalPathOrDefault(string physicalPath)
        {
            if (TryGetByPhysicalPath(physicalPath, out var result)) return result;
            return default;
        }

        public IReadOnlyList<ResourceType> GetSupportedTypes()
            => _supportedTypes ?? Array.Empty<ResourceType>();

        public bool IsSupported(ResourceType resourceType)
        {
            if (_supportedTypes == null
                || _supportedTypes.Length == 0) return true;

            for (var i = 0; i < _supportedTypes.Length; i++)
            {
                if (_supportedTypes[i] == resourceType) return true;
            }

            return false;
        }

        public abstract IEnumerable<Resource> SelectAll();

        public abstract bool TryGetByPhysicalPath(string physicalPath,
            [NotNullWhen(returnValue: true)] out Resource? result);

        protected Path1 GetVirtualPath(in Path1 internalPath)
        {
            var result = Path1
                .Join(VirtualBasePath, internalPath)
                .ToForm(VirtualPathForm);

            if (!result.IsInForm(MinimumVirtualPathForm))
            {
                throw Error.InvalidOperation("Resulting virtual path \"{0}\" doesn't conform to minimum virtual path form: {1}.",
                    result.ToString(), MinimumVirtualPathForm);
            }

            return result;
        }

        protected bool TryGetInternalPath(string physicalPath, out Path1 internalPath)
        {
            Path1 result;
            if (InternalBasePath.IsEmpty)
            {
                result = Path1.From(physicalPath, InternalPathForm);
            }
            else
            {
                result = Path1
                    .GetRelativePath(InternalBasePath, physicalPath)
                    .ToForm(InternalPathForm);
            }

            // This block attempts to escape from internal base path scope.
            // However, this is bit more aggressive, as it will also block
            // any physical paths which are not in normal form.
            if (!result.IsInForm(MinimumInternalPathForm))
            {
                internalPath = default;
                return false;
            }
            else
            {
                internalPath = result;
                return true;
            }
        }
    }
}
