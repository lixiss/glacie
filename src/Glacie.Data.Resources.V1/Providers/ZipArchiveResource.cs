using System.IO.Compression;

using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Data.Resources.V1.Providers
{
    internal sealed class ZipArchiveResource : GenericResource
    {
        private readonly ZipArchiveEntry _archiveEntry;

        public ZipArchiveResource(ResourceProvider provider,
            in Path1 virtualPath,
            string physicalPath,
            ResourceType type,
            ZipArchiveEntry archiveEntry)
            : base(provider, virtualPath, physicalPath, type)
        {
            Check.Argument.NotNull(archiveEntry, nameof(archiveEntry));

            _archiveEntry = archiveEntry;

            // Check what physicalPath is always original unmodified path.
            DebugCheck.That((object?)physicalPath == _archiveEntry.Name);
        }

        public ZipArchiveEntry ArchiveEntry => _archiveEntry;

        public override IO.Stream Open()
        {
            return ((ZipArchiveResourceProvider)Provider).OpenInternal(this);
        }
    }
}
