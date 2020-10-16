using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Glacie.GX1.Abstractions;

using IO = System.IO;

namespace Glacie.GX1.Discovery
{
    // TODO: module discovery is engine-type dependent (but can be unified)
    // for TQ we should pick different things (Audio, casing)
    // for GD we should pick different things (localization, not Audio, other casing)

    public sealed class ModuleDiscoverer
    {
        public ModuleInfo Discover(string path)
        {
            Check.Argument.NotNullNorEmpty(path, nameof(path));

            var physicalPath = IO.Path.GetFullPath(path);

            var result = new ModuleInfo(physicalPath);

            if (IO.Directory.Exists(physicalPath))
            {
                // Database Directory
                {
                    var databaseDir = FindDirectory(physicalPath, "?atabase", "Database");
                    if (databaseDir != null)
                    {
                        DiscoverDatabase(databaseDir, result);
                        DiscoverOverrideDirectories(databaseDir, result);
                    }
                }

                // Resources Directory
                {
                    var resourcesDir = FindDirectory(physicalPath, "?esources", "Resources");
                    if (resourcesDir != null)
                    {
                        DiscoverArcResourceBundles(resourcesDir, result);
                    }
                }

                // Audio Directory (TQ)
                {
                    var audioDir = FindDirectory(physicalPath, "?udio", "Audio");
                    if (audioDir != null)
                    {
                        result.SetEngineFamily(EngineFamily.TitanQuest);
                        DiscoverArcResourceBundles(audioDir, result);
                    }
                }

                // Text Directory (TQ)
                {
                    var textDir = FindDirectory(physicalPath, "?ext", "Text");
                    if (textDir != null)
                    {
                        result.SetEngineFamily(EngineFamily.TitanQuest);
                        DiscoverArcResourceBundles(textDir, result);
                    }
                }

                // TODO: handle Video directory for TQ/GD
                {
                    var videoDir = FindDirectory(physicalPath, "?ideo", "Video");
                    if (videoDir != null)
                    {
                        // TODO: handle Video directory for TQ/GD => need find references to video files
                        // and expose them as bundles (need know right prefix)
                    }
                }

                // Localization Directory
                {
                    var localizationDir = FindDirectory(physicalPath, "localization", "localization");
                    if (localizationDir != null)
                    {
                        result.SetEngineFamily(EngineFamily.GrimDawn);
                        // TODO: get GD localization resources, .zip files
                    }
                }
            }

            return result;
        }

        private static void DiscoverDatabase(string databaseDir, ModuleInfo result)
        {
            var files = IO.Directory.EnumerateFiles(databaseDir, "*.arz", IO.SearchOption.TopDirectoryOnly);
            var found = false;
            var ambigious = false;
            foreach (var databasePath in files)
            {
                if (found)
                {
                    ambigious = true;
                    break;
                }
                found = true;

                result.SetDatabasePath(databasePath);
            }
            if (ambigious) result.SetAmbigious();
        }

        private static void DiscoverArcResourceBundles(string resourcesDir, ModuleInfo result)
        {
            var files = IO.Directory.EnumerateFiles(resourcesDir, "*.arc", IO.SearchOption.AllDirectories);
            foreach (var bundlePhysicalPath in files)
            {
                var relativePath = IO.Path.GetRelativePath(resourcesDir, bundlePhysicalPath);

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

                prefix = AdjustPrefixCasing(prefix);

                result.AddBundle(ResourceBundleKind.Archive,
                    ModuleInfo.DefaultPriority,
                    bundlePhysicalPath,
                    prefix,
                    language);
            }
        }

        private void DiscoverOverrideDirectories(string basePath, ModuleInfo result)
        {
            var directories = IO.Directory.EnumerateDirectories(basePath, "*", IO.SearchOption.TopDirectoryOnly);
            foreach (var directory in directories)
            {
                string prefix = IO.Path.GetFileName(directory);

                if (!string.IsNullOrEmpty(prefix) && !IO.Path.EndsInDirectorySeparator(prefix))
                {
                    prefix += IO.Path.DirectorySeparatorChar;
                }

                prefix = AdjustPrefixCasing(prefix);


                string bundlePhysicalPath = directory;
                if (!IO.Path.EndsInDirectorySeparator(directory))
                {
                    bundlePhysicalPath += IO.Path.DirectorySeparatorChar;
                }

                result.AddBundle(ResourceBundleKind.Directory,
                    ModuleInfo.OverridePriority,
                    bundlePhysicalPath,
                    prefix,
                    language: null);
            }
        }

        private static string? FindDirectory(string modulePath, string searchPattern, string directoryName)
        {
            return IO.Directory.EnumerateDirectories(modulePath, searchPattern, IO.SearchOption.TopDirectoryOnly)
                .Where(x => IO.Path.GetFileName(x.AsSpan()).Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
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

        private static string AdjustPrefixCasing(string prefix)
        {
            const string xpackSegment = "XPack";

            if (string.IsNullOrEmpty(prefix)) return prefix;
            if (prefix.Length > xpackSegment.Length
                && !prefix.StartsWith(xpackSegment, StringComparison.Ordinal)
                && prefix.StartsWith(xpackSegment, StringComparison.OrdinalIgnoreCase)
                && (prefix[xpackSegment.Length] == '/' || prefix[xpackSegment.Length] == '\\'))
            {
                return xpackSegment + prefix.Substring(xpackSegment.Length);
            }

            return prefix;
        }
    }
}
