using System;

using Glacie.Data.Arc;
using Glacie.Data.Resources.Providers;

using IO = System.IO;

namespace Glacie.Resources
{
    public static class ResourceManagerExtensions
    {
        public static void AddForSourceDirectory(
            this ResourceManager resourceManager,
            string sourceDirectory,
            ushort sourceId,
            string? bundleNamePrefix = null)
        {
            if (!IO.Directory.Exists(sourceDirectory))
            {
                // Enumerate files with dummy search pattern, to throw DirectoryNotFoundException.
                IO.Directory.EnumerateFiles(sourceDirectory, Guid.NewGuid().ToString("N"));
                throw new IO.DirectoryNotFoundException("Specified source directory is not exist.");
            }

            AddResourceBundles(resourceManager, sourceDirectory, sourceId, bundleNamePrefix);
        }


        private static void AddResourceBundles(ResourceManager resourceManager, string sourceDirectory, ushort sourceId, string? bundleNamePrefix)
        {
            // TODO: handle per engine-type
            var resourcesDirectory1 = IO.Path.Combine(sourceDirectory, "resources");
            if (IO.Directory.Exists(resourcesDirectory1))
            {
                AddArcResourceBundlesFromDirectory(resourceManager, sourceDirectory, resourcesDirectory1, sourceId, bundleNamePrefix);
            }

            var audioDirectory1 = IO.Path.Combine(sourceDirectory, "audio");
            if (IO.Directory.Exists(audioDirectory1))
            {
                AddArcResourceBundlesFromDirectory(resourceManager, sourceDirectory, audioDirectory1, sourceId, bundleNamePrefix);
            }

            // TODO: handle local overrides inside source directory (should be enough to use local priority, but need custom flag)
        }

        private static void AddArcResourceBundlesFromDirectory(ResourceManager resourceManager,
            string sourceDirectory,
            string resourceDirectory,
            ushort sourceId,
            string? bundleNamePrefix)
        {
            foreach (var physicalPath in IO.Directory.EnumerateFiles(resourceDirectory, "*.arc", IO.SearchOption.AllDirectories))
            {
                var relativeBundleName = IO.Path.GetRelativePath(sourceDirectory, physicalPath);
                string bundleName;
                if (string.IsNullOrEmpty(bundleNamePrefix))
                {
                    bundleName = relativeBundleName;
                }
                else
                {
                    bundleName = IO.Path.Combine(bundleNamePrefix, relativeBundleName);
                }

                var relativePath = IO.Path.GetRelativePath(resourceDirectory, physicalPath);

                var archiveName = IO.Path.GetFileNameWithoutExtension(relativePath);
                string archivePrefix;
                string? language;
                if (!TryRemapArchiveNameToPrefixAndLanguage(archiveName, out archivePrefix, out language))
                {
                    archivePrefix = archiveName;
                    language = null;
                }

                string prefix;
                var directoryName = IO.Path.GetDirectoryName(relativePath);
                if (directoryName != null)
                {
                    prefix = IO.Path.Combine(directoryName, archivePrefix);
                }
                else
                {
                    prefix = archivePrefix;
                }

                if (!string.IsNullOrEmpty(prefix) && !IO.Path.EndsInDirectorySeparator(prefix))
                {
                    prefix += IO.Path.DirectorySeparatorChar;
                }

                var archive = ArcArchive.Open(physicalPath, ArcArchiveMode.Read);

                var bundle = new ArcArchiveBundle(
                    name: bundleName,
                    physicalPath: physicalPath,
                    supportedResourceTypes: null,
                    archive: archive,
                    disposeArchive: true
                    );

                resourceManager.AddBundle(prefix, language, bundleName, sourceId, bundle);
            }
        }

        private static bool TryRemapArchiveNameToPrefixAndLanguage(string archiveName, out string prefix, out string? language)
        {
            if (archiveName.Length > 3)
            {
                if (archiveName[archiveName.Length - 3] == '_')
                {
                    language = archiveName.Substring(archiveName.Length - 2);
                    prefix = archiveName.Substring(0, archiveName.Length - 3);
                    return true;
                }
            }

            prefix = archiveName;
            language = null;
            return false;
        }
    }
}
