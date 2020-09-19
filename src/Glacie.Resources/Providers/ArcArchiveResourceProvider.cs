using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;
using Glacie.Data.Arc;

using IO = System.IO;

namespace Glacie.Resources.Providers
{
    public sealed class ArcArchiveResourceProvider : ResourceProvider
    {
        private ArcArchive? _archive;
        private readonly VirtualPath _virtualRoot;
        private readonly bool _keepArchiveOpen;

        public ArcArchiveResourceProvider(string path,
            ResourceType[]? supportedResourceTypes,
            VirtualPath virtualRoot)
            : this(ArcArchive.Open(path, ArcArchiveMode.Read),
                  supportedResourceTypes: supportedResourceTypes,
                  virtualRoot: virtualRoot,
                  keepArchiveOpen: false)
        { }

        public ArcArchiveResourceProvider(ArcArchive archive,
            ResourceType[]? supportedResourceTypes,
            VirtualPath virtualRoot,
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

            foreach (var entry in _archive!.GetEntries())
            {
                VirtualPath name = entry.Name;
                var resourceType = ResourceTypeUtilities.FromName(name);
                if (IsResourceTypeSupported(resourceType))
                {
                    var vName = VirtualPath.Combine(_virtualRoot, name);
                    yield return new ArcArchiveResource(this, vName, entry);
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
                if (_archive!.TryGetEntry(vName, out var entry))
                {
                    result = new ArcArchiveResource(this, name, entry);
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

            if (resource is ArcArchiveResource aar)
            {
                return aar.Entry.Open();
            }
            else throw Error.Argument(nameof(resource), "Given resource has unknown type.");
        }
    }
}
