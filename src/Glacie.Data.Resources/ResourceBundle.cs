using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Glacie.Abstractions;
using Glacie.Utilities;

using IO = System.IO;

namespace Glacie.Data.Resources
{
    public abstract class ResourceBundle
        : IDisposable
        , IResourceBundle
    {
        private bool _disposed;
        private readonly string? _name;
        private readonly string? _physicalPath;
        private readonly ResourceType[]? _supportResourceTypes;

        protected ResourceBundle(
            string? name,
            string? physicalPath,
            IEnumerable<ResourceType>? supportedTypes)
        {
            _name = name;
            _physicalPath = physicalPath;
            _supportResourceTypes = GetHandledResourceTypes(supportedTypes);

            DebugCheck.That(_supportResourceTypes == null
                || _supportResourceTypes.Length > 0);
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

        public string? PhysicalPath => _physicalPath;

        public abstract IEnumerable<string> SelectAll();

        public abstract bool Exists(string path);

        public abstract IO.Stream Open(string path);

        public bool IsResourceTypeSupported(ResourceType value)
        {
            if (_supportResourceTypes == null) return true;
            for (var i = 0; i < _supportResourceTypes.Length; i++)
            {
                if (_supportResourceTypes[i] == value) return true;
            }
            return false;
        }

        public IReadOnlyList<ResourceType> GetSupportedResourceTypes()
            => _supportResourceTypes ?? Array.Empty<ResourceType>();

        private static ResourceType[]? GetHandledResourceTypes(IEnumerable<ResourceType>? supportedTypes)
        {
            if (supportedTypes == null) return null;

            var supportedTypesArray = supportedTypes
                .Where(x => x != ResourceType.None)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            if (supportedTypesArray.Length == 0) return null;

            return supportedTypesArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsPathSupported(string path)
        {
            if (_supportResourceTypes == null) return true;

            var resourceType = ResourceTypeUtilities.FromName(path);
            return IsResourceTypeSupported(resourceType);
        }

        protected void ThrowIfPathNotSupported(string path)
        {
            if (!IsPathSupported(path))
            {
                throw Error.InvalidOperation("Resource path \"{0}\" is not supported by this bundle.");
            }
        }
    }
}
