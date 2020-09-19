using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;

using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Resources.Providers
{
    public sealed class ZipArchiveResourceProvider : ResourceProvider
    {
        private ZipArchive? _archive;
        private readonly VirtualPath _virtualRoot;
        private readonly bool _keepArchiveOpen;

        public ZipArchiveResourceProvider(string path,
            ResourceType[]? supportedResourceTypes,
            in VirtualPath virtualRoot)
            : this(ZipFile.Open(path, ZipArchiveMode.Read),
                  supportedResourceTypes: supportedResourceTypes,
                  virtualRoot: virtualRoot,
                  keepArchiveOpen: false)
        { }

        public ZipArchiveResourceProvider(ZipArchive archive,
            ResourceType[]? supportedResourceTypes,
            in VirtualPath virtualRoot,
            bool keepArchiveOpen)
            : base(supportedResourceTypes)
        {
            Check.Argument.NotNull(archive, nameof(archive));

            _archive = archive;
            _virtualRoot = virtualRoot;
            _keepArchiveOpen = keepArchiveOpen;
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    if (!_keepArchiveOpen)
                    {
                        _archive?.Dispose();
                        _archive = null;
                    }
                }
            }
            base.Dispose(disposing);
        }

        public override IEnumerable<IResource> SelectAll()
        {
            ThrowIfDisposed();

            foreach (var entry in _archive!.Entries)
            {
                VirtualPath name = entry.FullName;
                var resourceType = ResourceTypeUtilities.FromName(name);
                if (IsResourceTypeSupported(resourceType))
                {
                    var vName = VirtualPath.Combine(_virtualRoot, name);
                    yield return new ZipArchiveResource(this, vName, entry);
                }
            }
        }

        public override bool TryGet(in VirtualPath name,
            [NotNullWhen(returnValue: true)] out IResource? result)
        {
            ThrowIfDisposed();

            // TODO: need normalization?
            var vName = name.TrimStartSegment(_virtualRoot, VirtualPathComparison.NonStandard);
            // var vName = name.Normalize(VirtualPathNormalization.Standard);

            var resourceType = ResourceTypeUtilities.FromName(vName);
            if (IsResourceTypeSupported(resourceType))
            {
                var entry = _archive!.Entries
                    .Where(x => vName.Equals(x.FullName, VirtualPathComparison.NonStandard))
                    .FirstOrDefault();
                if (entry != null)
                {
                    if (vName.Value.Contains("charstatstab2.tpl"))
                    {
                        ;
                    }

                    result = new ZipArchiveResource(this, name, entry);
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

            if (resource is ZipArchiveResource zar)
            {
                return zar.Entry.Open();
            }
            else throw Error.Argument(nameof(resource), "Given resource has unknown type.");
        }
    }
}
