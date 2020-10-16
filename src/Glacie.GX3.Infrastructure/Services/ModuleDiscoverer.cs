using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Discovery_Engines;
using Glacie.Localization;
using Glacie.Discovery_Modules;
using Glacie.Discovery_Modules.Builder;

using IO = System.IO;

namespace Glacie.Services.Discovery
{
    public sealed class ModuleDiscoverer : IModuleDiscoverer
    {
        // TODO: I want use IoC to construct this type and inject logger, if
        // this possible.
        //private readonly Logger _logger;
        //public ModuleDiscoverer(Logger<ModuleDiscoverer> logger)
        //{
        //    _logger = logger;
        //}

        // TODO: (Medium) (Module) (Engine) Perform discovery according to rules (which might be engine-dependent) EngineModuleLayout... ?
        // Also need deal with engine-dependent languages.
        public ModuleInfo DiscoverModule(string path)
        {
            Check.Argument.NotNullNorEmpty(path, nameof(path));

            var moduleDir = IO.Path.GetFullPath(path);

            var result = new ModuleInfoBuilder();
            result.SetPhysicalPath(moduleDir);

            if (IO.Directory.Exists(moduleDir))
            {
                // Database Directory
                {
                    var databaseDir = FindDirectory(moduleDir, "Database");
                    if (databaseDir != null)
                    {
                        DiscoverDatabase(result, databaseDir);
                        DiscoverOverrides(result, databaseDir);
                    }
                }

                // Resources Directory
                {
                    var resourcesDir = FindDirectory(moduleDir, "Resources");
                    if (resourcesDir != null)
                    {
                        DiscoverArchives(result, resourcesDir);
                    }
                }

                // Audio Directory (TQ)
                {
                    var audioDir = FindDirectory(moduleDir, "Audio");
                    if (audioDir != null)
                    {
                        result.AddEngineFamily(EngineFamily.TQ);
                        DiscoverArchives(result, audioDir);
                    }
                }

                // Text Directory (TQ)
                {
                    var textDir = FindDirectory(moduleDir, "Text");
                    if (textDir != null)
                    {
                        result.AddEngineFamily(EngineFamily.TQ);
                        DiscoverArchives(result, textDir);
                    }
                }

                // TODO: handle Video directory for TQ/GD
                {
                    var videoDir = FindDirectory(moduleDir, "Video");
                    if (videoDir != null)
                    {
                        // TODO: handle Video directory for TQ/GD => need find references to video files
                        // and expose them as bundles (need know right prefix)
                    }
                }

                // Localization Directory
                {
                    var localizationDir = FindDirectory(moduleDir, "localization");
                    if (localizationDir != null)
                    {
                        result.AddEngineFamily(EngineFamily.GD);
                        // TODO: get GD localization resources, .zip files
                    }
                }
            }

            return result.Build();
        }

        private static void DiscoverDatabase(ModuleInfoBuilder result, string databaseDir)
        {
            var files = IO.Directory.EnumerateFiles(databaseDir, "*.arz", IO.SearchOption.TopDirectoryOnly);
            foreach (var databasePath in files)
            {
                result.AddDatabase(databasePath);
            }
        }

        private static void DiscoverArchives(ModuleInfoBuilder result, string resourcesDir)
        {
            var files = IO.Directory.EnumerateFiles(resourcesDir, "*.arc", IO.SearchOption.AllDirectories);
            foreach (var bundlePhysicalPath in files)
            {
                var relativePath = IO.Path.GetRelativePath(resourcesDir, bundlePhysicalPath);

                var archiveName = IO.Path.GetFileNameWithoutExtension(relativePath);
                string? archivePrefix;
                Language? language;
                if (!TryRemapArchiveNameToPrefixAndLanguage(archiveName, out archivePrefix, out language))
                {
                    archivePrefix = archiveName;
                    language = Language.Invariant;
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

                result.AddResourceBundle(bundlePhysicalPath,
                    ResourceBundleKind.Archive,
                    ModuleInfoBuilder.DefaultPriorityClass,
                    prefix,
                    language);
            }
        }

        private void DiscoverOverrides(ModuleInfoBuilder result, string basePath)
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

                result.AddResourceBundle(
                    bundlePhysicalPath, 
                    ResourceBundleKind.Directory,
                    ModuleInfoBuilder.OverridePriorityClass,
                    prefix,
                    Language.Invariant);
            }
        }

        private static string? FindDirectory(string path, string directoryName)
        {
            return IO.Directory.EnumerateDirectories(path, GetSearchPattern(directoryName), IO.SearchOption.TopDirectoryOnly)
                .Where(x => IO.Path.GetFileName(x.AsSpan()).Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        private static bool TryRemapArchiveNameToPrefixAndLanguage(
            string archiveName,
            [NotNullWhen(true)] out string? prefix,
            [NotNullWhen(true)] out Language? language)
        {
            if (archiveName.Length > 3)
            {
                if (archiveName[archiveName.Length - 3] == '_')
                {
                    var languageCode = archiveName.Substring(archiveName.Length - 2);
                    prefix = archiveName.Substring(0, archiveName.Length - 3);

                    // TODO: There is engine-dependent mapping, and i want ask engine type about,
                    // instead internal language map. Also module discoverer might produce
                    // diagnostic errors for this case (e.g. if it can't map language).
                    language = Language.GetLanguages()
                        .Where(x => languageCode.Equals(x.Code, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    if (language == null)
                    {
                        throw Error.NotImplemented("Archive \"{0}\" looks like language-dependent resource bundle, but " +
                            "engine-driven language mappings is not implemented yet.", archiveName);

                        prefix = null;
                        language = null;
                        return false;
                    }

                    return true;
                }
            }

            prefix = null;
            language = null;
            return false;
        }

        private static string GetSearchPattern(string name)
        {
            switch (name)
            {
                case "Database": return "?atabase";
                case "Resources": return "?esources";
                case "Audio": return "?udio";
                case "Text": return "?ext";
                case "Video": return "?ideo";
                case "localization": return "localization";
                default: return "?" + name.Substring(1);
            }
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
