using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Data.Resources.V1.Providers
{
    internal sealed class FileSystemResource : GenericResource
    {
        public FileSystemResource(ResourceProvider provider,
            in Path1 virtualPath,
            string physicalPath,
            ResourceType type)
            : base(provider, virtualPath, physicalPath, type)
        {
        }

        public override IO.Stream Open()
        {
            return ((FileSystemResourceProvider)Provider).OpenInternal(this);
        }
    }
}
