using Glacie.Abstractions;
using Glacie.Metadata;
using Glacie.Metadata.Resolvers;

namespace Glacie.Targeting.TQAE
{
    public sealed class TqaeMetadataResolver : DefaultMetadataResolver
    {
        private static readonly ResolutionToken _remappedCustomMapsArtTqx3 = new ResolutionToken("TQAE.TemplateName.CustomMapsArtTqx3");

        public TqaeMetadataResolver(MetadataProvider metadataProvider, bool disposeMetadataProvider)
            : base(metadataProvider, disposeMetadataProvider)
        { }

        public override Resolution<RecordType> ResolveRecordTypeByTemplateName(Path templateName)
        {
            var resolution = base.ResolveRecordTypeByTemplateName(templateName);
            if (resolution.HasValue) return resolution;

            if (templateName.TryTrimStart(Path.Implicit("Custommaps/Art_TQX3"), PathComparison.OrdinalIgnoreCase, out var remapped))
            {
                resolution = base.ResolveRecordTypeByTemplateName(remapped);
                if (resolution.HasValue)
                {
                    return resolution.WithToken(_remappedCustomMapsArtTqx3);
                }
            }

            return resolution;
        }
    }
}
