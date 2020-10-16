using Glacie.Metadata;

namespace Glacie.Configuration
{
    public sealed class ContextMetadataConfiguration
    {
        public string? Path { get; set; }

        // TODO: Should be MetadataProvider
        public MetadataResolver? MetadataResolver { get; set; }
    }
}
