using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Data.Resources.V1.Providers
{
    public sealed class ZipArchiveResourceProvider : ResourceProvider
    {
        private ZipArchive? _archive;
        private readonly bool _disposeArchive;

        public ZipArchiveResourceProvider(
            string? name,
            in Path1 virtualBasePath,
            Path1Form virtualPathForm,
            in Path1 internalBasePath,
            Path1Form internalPathForm,
            IEnumerable<ResourceType>? supportedTypes,
            ZipArchive archive,
            bool disposeArchive)
            : base(name,
                  virtualBasePath,
                  virtualPathForm,
                  internalBasePath,
                  internalPathForm,
                  supportedTypes)
        {
            Check.Argument.NotNull(archive, nameof(archive));
            _archive = archive;
            _disposeArchive = disposeArchive;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!Disposed)
                {
                    if (_disposeArchive)
                    {
                        _archive?.Dispose();
                        _archive = null;
                    }
                }
            }
            base.Dispose(disposing);
        }


        public override IEnumerable<Resource> SelectAll()
        {
            ThrowIfDisposed();

            foreach (var entry in _archive!.Entries)
            {
                if (TryCreateResource(entry, out var resource))
                {
                    yield return resource;
                }
            }
        }

        public override bool TryGetByPhysicalPath(string physicalPath,
            [NotNullWhen(true)] out Resource? result)
        {
            ThrowIfDisposed();

            var archiveEntry = _archive!.GetEntry(physicalPath);
            if (archiveEntry != null)
            {
                if (TryCreateResource(archiveEntry, out var resource))
                {
                    result = resource;
                    return true;
                }
            }

            result = default;
            return false;
        }

        internal IO.Stream OpenInternal(ZipArchiveResource resource)
        {
            ThrowIfDisposed();

            return resource.ArchiveEntry.Open();
        }

        private bool TryCreateResource(ZipArchiveEntry entry,
            [NotNullWhen(returnValue: true)] out ZipArchiveResource? result)
        {
            var physicalPath = entry.FullName;
            if (!TryGetInternalPath(physicalPath, out var internalPath))
            {
                result = null;
                return false;
            }

            var virtualPath = GetVirtualPath(internalPath);
            var resourceType = ResourceTypeUtilities.FromPath(virtualPath);
            if (IsSupported(resourceType))
            {
                result = new ZipArchiveResource(this,
                    virtualPath,
                    physicalPath,
                    resourceType,
                    entry);
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
