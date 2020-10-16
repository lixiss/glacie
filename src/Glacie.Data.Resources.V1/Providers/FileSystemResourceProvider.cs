using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Data.Resources.V1.Providers
{
    public sealed class FileSystemResourceProvider : ResourceProvider
    {
        private readonly string _basePath;

        public FileSystemResourceProvider(
            string? name,
            in Path1 virtualBasePath,
            Path1Form virtualPathForm,
            in Path1 internalBasePath,
            Path1Form internalPathForm,
            IEnumerable<ResourceType>? supportedTypes,
            string basePath)
            : base(name,
                  virtualBasePath,
                  virtualPathForm,
                  internalBasePath,
                  internalPathForm | Path1Form.Relative,
                  supportedTypes)
        {
            _basePath = IO.Path.GetFullPath(basePath);
        }

        public override IEnumerable<Resource> SelectAll()
        {
            ThrowIfDisposed();

            var basePath = _basePath;
            if (!InternalBasePath.IsEmpty)
            {
                basePath = IO.Path.Combine(basePath, InternalBasePath.ToString());
            }

            if (IO.Directory.Exists(basePath))
            {
                return SelectAllInternal(basePath);
            }

            return Enumerable.Empty<Resource>();
        }

        private IEnumerable<Resource> SelectAllInternal(string basePath)
        {
            var searchPattern = ResourceTypeUtilities.GetSearchPattern(GetSupportedTypes());

            var files = IO.Directory.EnumerateFiles(basePath, searchPattern, IO.SearchOption.AllDirectories);
            foreach (var fullPath in files)
            {
                DebugCheck.That(IO.Path.IsPathFullyQualified(fullPath));

                if (TryCreateResource(fullPath, out var resource))
                {
                    yield return resource;
                }
            }
        }

        public override bool TryGetByPhysicalPath(string physicalPath,
            [NotNullWhen(true)] out Resource? result)
        {
            ThrowIfDisposed();

            var fullPath = IO.Path.Combine(_basePath, physicalPath);

            if (IO.File.Exists(fullPath))
            {
                if (TryCreateResource(fullPath, out var resource))
                {
                    result = resource;
                    return true;
                }
            }

            result = default;
            return false;
        }

        internal IO.Stream OpenInternal(FileSystemResource resource)
        {
            ThrowIfDisposed();

            var fullPath = IO.Path.Combine(_basePath, resource.PhysicalPath);
            return IO.File.OpenRead(fullPath);
        }

        private bool TryCreateResource(string fullPath,
            [NotNullWhen(returnValue: true)] out FileSystemResource? result)
        {
            var physicalPath = IO.Path.GetRelativePath(_basePath, fullPath);
            if (!TryGetInternalPath(physicalPath, out var internalPath))
            {
                result = null;
                return false;
            }

            var virtualPath = GetVirtualPath(internalPath);
            var resourceType = ResourceTypeUtilities.FromPath(virtualPath);
            if (IsSupported(resourceType))
            {
                result = new FileSystemResource(this,
                    virtualPath,
                    physicalPath,
                    resourceType);
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
