using IO = System.IO;

namespace Glacie.Abstractions
{
    public abstract class Resource : IResource
    {
        protected Resource() { }

        public abstract VirtualPath Name { get; }

        public abstract ResourceType Type { get; }

        public abstract bool Development { get; }

        public abstract IO.Stream Open();
    }
}
