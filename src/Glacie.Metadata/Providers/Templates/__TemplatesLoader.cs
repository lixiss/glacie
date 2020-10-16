using System;

using Glacie.Logging;
using Glacie.Metadata.Builder;
using Glacie.Resources;
using Glacie.Targeting;

namespace Glacie.Metadata.Builders.Templates
{
    internal sealed class __TemplatesLoader
    {
        private readonly EngineType _engineType;
        private readonly Logger? _log;

        public __TemplatesLoader(EngineType engineType, Logger? logger)
        {
            Check.Argument.NotNull(engineType, nameof(engineType));

            _engineType = engineType;
            _log = logger ?? Logger.Null;
        }

        public __MetadataProvider CreateProvider(string path)
        {
            // TODO: Ensure owning chaing

            var resourceManager = new ResourceManager(language: null, logger: _log);
            var bdr = new __TemplatesResourceBundleDiscoverer(lazyBundleCreation: false).Discover(path);
            if (bdr.IsFactory)
            {
                resourceManager.AddBundle(bdr.Prefix, language: null, null, 0, bdr.BundleFactory);
            }
            else
            {
                var bundleName = bdr.Bundle.Name;
                resourceManager.AddBundle(bdr.Prefix, language: null, bundleName, 0, bdr.Bundle);
            }

            var resourceProvider = (ResourceProvider)resourceManager;

            // 2. Configure template provider to parse templates.
            var templateProvider = new __obs__TemplateProvider(resourceProvider, disposeProvider: true);

            // 3. Configure template resolver to find correct templates.
            // Resolver should apply path mapping...
            // 1. should use shared mapping logic with resource manager
            // Also, it should process some additional logic:
            // e.g.: trimming %TEMPLATE_DIR%,
            // trim: custommaps/art_tqx3 (TQAE)
            var templateResolver = new __obs__TemplateResolver(templateProvider);


            // 5. Create TemplateMetadataProvider -> provide builders.
            var metadataProvider = new __TemplateMetadataProvider(
                templateResolver,
                templateProcessor: _engineType.GetTemplateProcessor(),
                _log);
            // TODO: metadata provider should provide DatabaseTypeBuilder

            foreach (var x in templateResolver.SelectAll())
            {
                // if (x.Type != Abstractions.ResourceType.Template) continue;

                var recordType = metadataProvider.GetRecordTypeBuilder(x.Path);
                Console.Out.WriteLine("Loading: {0}", x.Path);
            }

            return metadataProvider;
        }
    }
}
