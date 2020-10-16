using Glacie.Abstractions;
using Glacie.Data.Resources;
using Glacie.Utilities;

using IO = System.IO;

namespace Glacie.Resources
{
    public sealed class Resource
    {
        private readonly Path _path;
        private readonly ResourceBundle _bundle;
        private readonly string _bundleResourcePath;

        private readonly ushort _sourceId;
        private readonly ushort _localPriority;

        internal Resource(
            Path path,
            ResourceBundle provider,
            string bundleResourcePath,
            ushort sourceId,
            ushort localPriority)
        {
            _path = path;
            _bundle = provider;
            _bundleResourcePath = bundleResourcePath;

            _sourceId = sourceId;
            _localPriority = localPriority;
        }

        public string Name => _path.ToString();

        public Path Path => _path;

        public ResourceType Type => ResourceTypeUtilities.FromPath(_path);

        public ushort SourceId => _sourceId;

        internal ushort LocalPriority => _localPriority;

        internal uint Priority => ResourceManager.GetGlobalPriority(_sourceId, _localPriority);

        internal ResourceBundle Bundle => _bundle;

        internal string ResourceName => _bundleResourcePath;

        public IO.Stream Open() => _bundle.Open(_bundleResourcePath);

        // TODO: Timestamp, LastWriteTimeUtc, Length, etc...
    }
}
