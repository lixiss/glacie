using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

using Glacie.Data.Arc;
using Glacie.Data.Resources;
using Glacie.Data.Resources.Providers;
using Glacie.Localization;
using Glacie.Logging;
using Glacie.Modules;
using Glacie.Resources;

namespace Glacie.Infrastructure
{
    [Obsolete("There is temporary adapter, new resource system needed anyway. This also somewhy live in ProjectSystem.")]
    public sealed class ResourceProviderFactory
    {
        // TODO: Correct support of languages needed.

        // TODO: Should be ResourceProvider, and ResourceResolver.
        public ResourceManager CreateResourceManager(IEnumerable<Module> modules, Logger logger)
        {
            var resourceManager = new ResourceManager(null, logger);

            var sourceId = 0;
            foreach (var module in modules)
            {
                var resourceSet = module.ResourceSet;

                ProcessForLanguage(resourceManager, resourceSet, sourceId, LanguageSymbol.Invariant);
                ProcessForLanguage(resourceManager, resourceSet, sourceId, LanguageSymbol.English);

                sourceId++;
            }

            return resourceManager;
        }

        private static void ProcessForLanguage(ResourceManager result, IResourceBundleSet resourceSet, int sourceId, LanguageSymbol language)
        {
            foreach (var resourceBundle in resourceSet.Select(language))
            {
                var languageCode = resourceBundle.Language.LanguageInfo.Code;
                if (string.IsNullOrEmpty(languageCode)) languageCode = null;

                // TODO: overrides doesn't supported properly
                result.AddBundle(
                    resourceBundle.Prefix,
                    languageCode,
                    resourceBundle.PhysicalPath,
                    checked((ushort)sourceId),
                    () =>
                    {
                        switch (resourceBundle.Kind)
                        {
                            case ResourceBundleKind.FileSystem:
                                return new FileSystemBundle(name: null,
                                    physicalPath: resourceBundle.PhysicalPath,
                                    supportedResourceTypes: null);

                            case ResourceBundleKind.ArcArchive:
                                return new ArcArchiveBundle(name: null,
                                    physicalPath: resourceBundle.PhysicalPath,
                                    supportedResourceTypes: null,
                                    archive: ArcArchive.Open(resourceBundle.PhysicalPath, ArcArchiveMode.Read),
                                    disposeArchive: true);

                            case ResourceBundleKind.ZipArchive:
                                return new ZipArchiveBundle(name: null,
                                    physicalPath: resourceBundle.PhysicalPath,
                                    supportedResourceTypes: null,
                                    archive: ZipFile.Open(resourceBundle.PhysicalPath, ZipArchiveMode.Read),
                                    disposeArchive: true);

                            default:
                                throw Error.Argument(nameof(ResourceBundleKind));
                        }
                    });
            }
        }
    }
}
