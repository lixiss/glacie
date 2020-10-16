using System;
using System.Collections.Generic;

using Glacie.Discovery;
using Glacie.Logging;
using Glacie.Metadata.Builder;
using Glacie.Metadata.Providers;
using Glacie.Metadata.Serialization;
using Glacie.Resources;
using Glacie.Targeting;

using IO = System.IO;

namespace Glacie.Metadata.Factories
{
    public static class MetadataProviderFactory
    {
        // TODO: MetadataProvider should return associated EngineType (if it was discovered)
        public static MetadataProvider Create(List<string> path, EngineType? engineType, Logger? logger = null)
        {
            Check.Argument.NotNull(path, nameof(path));

            foreach (var p in path) Check.Argument.NotNullNorWhiteSpace(p, nameof(path));

            var metadataBuilder = new MetadataBuilder();
            foreach (var p in path)
            {
                var provider = LoadTo(metadataBuilder, lazyLoading: false, p, engineType, logger);
                provider?.Dispose();
            }
            return metadataBuilder.Build();
        }

        public static MetadataProvider Create(string path, EngineType? engineType, Logger? logger = null, bool loadLazily = true)
        {
            Check.Argument.NotNullNorWhiteSpace(path, nameof(path));

            var metadataBuilder = new MetadataBuilder();
            return LoadTo(metadataBuilder, loadLazily, path, engineType, logger) ?? metadataBuilder.Build();
        }

        /// <summary>
        /// Create provider and optionally populate MetadataBuilder.
        /// Optionally return MetadataProvider which can be immediately disposed.
        /// </summary>
        private static MetadataProvider? LoadTo(MetadataBuilder metadataBuilder, bool lazyLoading, string path, EngineType? engineType, Logger? logger = null)
        {
            // TODO: Here is good place to determine what path is. Need common path discovery.
            // Discover for GXMD or GXMP

            if (Path.GetExtension(path).Equals(".gxm", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: check what file exist
                if (!IO.File.Exists(path))
                {
                    throw Error.InvalidOperation("File \"{0}\" not found...", path);
                }

                var reader = new MetadataModuleReader();
                var list = reader.Read(IO.File.OpenRead(path), path);
                foreach (var p in list)
                {
                    var provider = LoadTo(metadataBuilder, lazyLoading: false, p, engineType, logger);
                    provider?.Dispose();
                }
                return null;
            }
            else if (Path.GetExtension(path).Equals(".gxmd", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(path).Equals(".gxmp", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: check what file exist
                if (!IO.File.Exists(path))
                {
                    throw Error.InvalidOperation("File \"{0}\" not found...", path);
                }

                var reader = new MetadataReader(metadataBuilder, new MetadataReaderOptions
                {
                    ResourceResolver = null, // filesystem
                });

                reader.Read(path);

                return null;
            }

            // templates
            {
                var discoverer = new TemplatesResourceBundleDiscoverer(lazyBundleCreation: true);
                var bdr = discoverer.Discover(path);

                if (metadataBuilder.BasePath.IsEmpty)
                {
                    metadataBuilder.BasePath = Path.Implicit("Database/Templates");
                }

                var resourceManager = new ResourceManager(language: null, logger: logger);

                if (bdr.IsFactory)
                {
                    resourceManager.AddBundle(bdr.Prefix, language: null, null, 0, bdr.BundleFactory);
                }
                else
                {
                    var bundleName = bdr.Bundle.Name;
                    resourceManager.AddBundle(bdr.Prefix, language: null, bundleName, 0, bdr.Bundle);
                }

                if (engineType == null)
                {
                    engineType = DetectEngineTypeFromTemplateResources(resourceManager.AsResolver(takeOwnership: false));
                }

                var provider = new TemplateMetadataProvider(metadataBuilder, engineType, resourceManager.AsResolver(takeOwnership: true), logger);
                if (!lazyLoading) provider.PopulateMetadataBuilder();
                return provider;
            }
        }

        private static EngineType DetectEngineTypeFromTemplateResources(IResourceResolver resourceResolver)
        {
            if (EngineTypeDiscoverer.TryGetEngineTypeId(resourceResolver, out var engineTypeId))
            {
                return EngineType.GetFrom(engineTypeId);
            }
            else
            {
                throw Error.InvalidOperation("Engine type is not specified, and can't be inferred.");
            }
        }
    }
}
