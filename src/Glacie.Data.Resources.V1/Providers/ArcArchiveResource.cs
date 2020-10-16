using Glacie.Abstractions;
using Glacie.Data.Arc;

using IO = System.IO;

namespace Glacie.Data.Resources.V1.Providers
{
    internal sealed class ArcArchiveResource : GenericResource
    {
        private readonly ArcArchiveEntry _archiveEntry;

        public ArcArchiveResource(ResourceProvider provider,
            in Path1 virtualPath,
            string physicalPath,
            ResourceType type,
            ArcArchiveEntry archiveEntry)
            : base(provider, virtualPath, physicalPath, type)
        {
            // Check.Argument.NotNull(internalPath, nameof(internalPath));

            _archiveEntry = archiveEntry;

            // Check what physicalPath is always original unmodified path.
            DebugCheck.That((object?)physicalPath == _archiveEntry.Name);
        }

        public ArcArchiveEntry ArchiveEntry => _archiveEntry;

        public override IO.Stream Open()
        {
            return ((ArcArchiveResourceProvider)Provider).OpenInternal(this);
        }
    }
}
