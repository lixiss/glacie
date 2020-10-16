using Glacie.Abstractions;

namespace Glacie.Metadata.Resolvers
{
    public class DefaultMetadataResolver : MetadataResolver
    {
        private MetadataProvider _metadataProvider;
        private readonly bool _disposeMetadataProvider;

        public DefaultMetadataResolver(MetadataProvider metadataProvider, bool disposeMetadataProvider)
        {
            Check.Argument.NotNull(metadataProvider, nameof(metadataProvider));
            _metadataProvider = metadataProvider;
            _disposeMetadataProvider = disposeMetadataProvider;
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (_disposeMetadataProvider)
                    {
                        _metadataProvider?.Dispose();
                        _metadataProvider = null!;
                    }
                }

                base.Dispose(disposing);
            }
        }


        public override DatabaseType GetDatabaseType()
        {
            return _metadataProvider.GetDatabaseType();
        }

        public override Resolution<RecordType> ResolveRecordTypeByName(string name)
        {
            if (GetDatabaseType().TryGetRecordTypeByName(name, out var result))
            {
                return new Resolution<RecordType>(result, true);
            }
            else
            {
                return default;
            }
        }

        public override Resolution<RecordType> ResolveRecordTypeByPath(Path path)
        {
            if (GetDatabaseType().TryGetRecordTypeByPath(path, out var result))
            {
                return new Resolution<RecordType>(result, true);
            }
            else
            {
                return default;
            }
        }

        public override Resolution<RecordType> ResolveRecordTypeByTemplateName(Path templateName)
        {
            if (GetDatabaseType().TryGetRecordTypeByTemplateName(templateName, out var result))
            {
                return new Resolution<RecordType>(result, true);
            }
            else return default;
        }
    }
}
