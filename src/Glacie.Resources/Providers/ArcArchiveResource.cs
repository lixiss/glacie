using Glacie.Data.Arc;

using IO = System.IO;

namespace Glacie.Resources.Providers
{
    internal class ArcArchiveResource : ResourceBase
    {
        private readonly ArcEntry _entry;

        public ArcArchiveResource(ArcArchiveResourceProvider provider,
            in VirtualPath name,
            ArcEntry entry)
            : base(provider, name)
        {
            _entry = entry;
        }

        public ArcEntry Entry => _entry;

        public override IO.Stream Open()
        {
            return _entry.Open();
        }
    }
}
