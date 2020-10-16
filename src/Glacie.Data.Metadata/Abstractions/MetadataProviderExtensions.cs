using Glacie.Metadata.Resolvers;

namespace Glacie.Metadata
{
    public static class MetadataProviderExtensions
    {
        public static MetadataResolver AsResolver(this MetadataProvider metadataProvider, bool takeOwnership = false)
        {
            return new DefaultMetadataResolver(metadataProvider, disposeMetadataProvider: takeOwnership);
        }
    }
}
