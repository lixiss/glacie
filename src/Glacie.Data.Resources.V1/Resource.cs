using IO = System.IO;

namespace Glacie.Data.Resources.V1
{
    public abstract class Resource : IResource
    {
        protected Resource() { }

        public abstract string Name { get; }

        public abstract ref readonly Path1 VirtualPath { get; }

        public abstract string PhysicalPath { get; }

        public abstract ResourceType Type { get; }

        public abstract bool Development { get; }

        public abstract ResourceProvider Provider { get; }

        IResourceProvider IResource.Provider => Provider;

        public abstract IO.Stream Open();
    }
}
