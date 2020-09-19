using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Resources.Providers
{
    // TODO: support VirtualRootPath in ResourceProvider?

    public sealed class FileSystemResourceProvider : ResourceProvider
    {
        private readonly string _basePath;
        private readonly string? _virtualBasePath;

        public FileSystemResourceProvider(string basePath,
            ResourceType[]? supportedResourceTypes,
            string? virtualBasePath)
            : base(supportedResourceTypes)
        {
            Check.Argument.NotNull(basePath, nameof(basePath));

            if (!IO.Directory.Exists(basePath))
            {
                throw Error.InvalidOperation("Invalid base path specified.");
            }

            _basePath = basePath;
            _virtualBasePath = virtualBasePath;
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // ...
                }
            }
            base.Dispose(disposing);
        }

        public override IEnumerable<IResource> SelectAll()
        {
            ThrowIfDisposed();

            var searchPattern = ResourceTypeUtilities.GetSearchPattern(GetSupportedResourceTypes());
            var files = IO.Directory.EnumerateFiles(_basePath, searchPattern, IO.SearchOption.AllDirectories);

            foreach (var path in files)
            {
                var name = GetResourceName(path);
                var resourceType = ResourceTypeUtilities.FromName(name);
                if (IsResourceTypeSupported(resourceType))
                {
                    yield return new FileSystemResource(this, name, IO.Path.GetFullPath(path));
                }
            }
        }

        public override bool TryGet(in VirtualPath name,
            [NotNullWhen(returnValue: true)] out IResource? result)
        {
            ThrowIfDisposed();

            var resourceType = ResourceTypeUtilities.FromName(name);
            if (IsResourceTypeSupported(resourceType))
            {
                var filePath = GetFilePath(name);
                if (IO.File.Exists(filePath))
                {
                    result = new FileSystemResource(this, name, filePath);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
            else
            {
                throw Error.InvalidOperation("This provider doesn't support specified resource.");
            }
        }

        public override IO.Stream Open(in VirtualPath name)
        {
            ThrowIfDisposed();

            var resource = Get(name);
            return Open(resource);
        }

        public override IO.Stream Open(IResource resource)
        {
            ThrowIfDisposed();

            if (resource is ResourceBase rb)
            {
                if ((object)rb.Provider != this)
                {
                    throw Error.Argument(nameof(resource),
                        "Given resource doesn't belongs to this provider.");
                }
            }

            if (resource is FileSystemResource fsr)
            {
                return IO.File.Open(fsr.FilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read);
            }
            else throw Error.Argument(nameof(resource), "Given resource has unknown type.");
        }

        private string GetFilePath(string resourceName)
        {
            // TODO: validate / normalize resource name, it comes from external

            if (string.IsNullOrEmpty(_virtualBasePath))
            {
                return IO.Path.Combine(_basePath, resourceName);
            }
            else
            {
                var relativePath = IO.Path.GetRelativePath(_virtualBasePath, resourceName);
                return IO.Path.Combine(_basePath, relativePath);
            }
        }

        private string GetResourceName(string path)
        {
            // TODO: normalize resource name, e.g. path separators should be /
            // TODO: ensure what resource name is valid (e.g. not rooted, has no invalid segments, etc)

            if (string.IsNullOrEmpty(_virtualBasePath))
            {
                return IO.Path.GetRelativePath(_basePath, path);
            }
            else
            {
                var relativePath = IO.Path.GetRelativePath(_basePath, path);
                return IO.Path.Combine(_virtualBasePath, relativePath);
            }
        }
    }
}
