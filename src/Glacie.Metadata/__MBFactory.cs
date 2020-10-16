using Glacie.Abstractions;
using Glacie.Data.Resources.Providers;
using Glacie.Metadata.Builder;
using Glacie.Metadata.Builders.Glacie;
using Glacie.Resources;

using IO = System.IO;

namespace Glacie.Metadata
{
    public sealed class __MBFactory
    {
        public static __MetadataBuilder Create(string path)
        {
            if (IsGxmdPath(path))
            {
                if (IO.File.Exists(path))
                {
                    // Load .gxmd or set of .gxmd files.
                    // There is bit too complex, because i'm also want to read
                    // this resources from archive in uniform way.
                    var directoryPath = IO.Path.GetDirectoryName(path);
                    var relativePathInBundle = IO.Path.GetFileName(path);
                    Check.That(directoryPath != null);
                    var bundle = new FileSystemBundle("gx-metadata",
                        string.IsNullOrEmpty(directoryPath) ? "." : directoryPath,
                        new[] { ResourceType.GlacieMetadata, ResourceType.GlacieMetadataInclude });
                    var resourceManager = new ResourceManager(language: null, logger: null);
                    resourceManager
                        .AddBundle(prefix: "",
                            language: null,
                            bundleName: null,
                            sourceId: 0,
                            bundle);
                    var resolver = resourceManager.AsResolver(takeOwnership: true);
                    return new __GxmdMetadataBuilder(resolver, Path1.From(relativePathInBundle));
                }
                else
                {
                    // When .gxmd file is not exist, there is not necessary error,
                    // there is possible directory with this suffix.
                    throw Error.NotImplemented();
                }
            }
            else throw Error.NotImplemented();
        }

        private static bool IsGxmdPath(string path)
        {
            return path.EndsWith(".gxmd");
        }

        private static bool IsGxmpPath(string path)
        {
            return path.EndsWith(".gxmp");
        }
    }
}
