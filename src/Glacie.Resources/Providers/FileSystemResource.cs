using Glacie.Abstractions;

namespace Glacie.Resources.Providers
{
    internal sealed class FileSystemResource : ResourceBase
    {
        private readonly string _filePath;

        public FileSystemResource(ResourceProvider provider, in VirtualPath name, string filePath)
            : base(provider, name)
        {
            Check.Argument.NotNull(filePath, nameof(filePath));
            _filePath = filePath;
        }

        internal string FilePath => _filePath;
    }
}
