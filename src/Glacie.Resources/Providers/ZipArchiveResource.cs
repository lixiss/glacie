using System.IO.Compression;

using IO = System.IO;

namespace Glacie.Resources.Providers
{
    internal class ZipArchiveResource : ResourceBase
    {
        private readonly ZipArchiveEntry _entry;

        public ZipArchiveResource(ZipArchiveResourceProvider provider,
            in VirtualPath name,
            ZipArchiveEntry entry)
            : base(provider, name)
        {
            _entry = entry;
        }

        public ZipArchiveEntry Entry => _entry;

        public override IO.Stream Open()
        {
            return _entry.Open();
        }
    }
}
