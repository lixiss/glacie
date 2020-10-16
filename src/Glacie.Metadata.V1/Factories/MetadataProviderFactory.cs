using System;

using Glacie.Data.Arz;

using IO = System.IO;

namespace Glacie.Metadata.V1
{
    public static class MetadataProviderFactory
    {
        public static MetadataProvider Create(string path)
            => Create(path, options: null);

        public static MetadataProvider Create(string path, MetadataProviderFactoryOptions? options)
        {
            if (IO.File.Exists(path))
            {
                if (path.EndsWith(".arz", StringComparison.OrdinalIgnoreCase))
                {
                    var database = ArzDatabase.Open(path, options?.ArzReaderOptions);
                    return new EphemeralMetadataProvider(database, options?.Logger, true);
                }

                // TODO: Support .gxm
            }

            var templateResourceProvider = TemplateResourceProviderFactory.Create(path);
            var templateProvider = TemplateProviderFactory.Create(templateResourceProvider);

            return new TemplateMetadataProvider(templateProvider,
                templateNameMapper: options?.TemplateNameMapper,
                templateProcessor: options?.TemplateProcessor,
                logger: options?.Logger);
        }

        // TODO: Create from resource provider
    }
}
