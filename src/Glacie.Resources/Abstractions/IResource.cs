using IO = System.IO;

namespace Glacie.Abstractions
{
    public interface IResource
    {
        public VirtualPath Name { get; }

        public ResourceType Type { get; }

        // TODO: location (e.g. archive, or archive path or file system path)

        /// <summary>
        /// Development-time asset. Template asset is one of such resource.
        /// </summary>
        public bool Development { get; }

        IO.Stream Open();
    }
}
