using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

using Glacie.Abstractions;
using Glacie.Data.Arc;
using Glacie.Data.Resources;
using Glacie.Data.Resources.Providers;

using IO = System.IO;

namespace Glacie.Metadata.Factories
{
    internal sealed class TemplatesResourceBundleDiscoverer
    {
        private const string DatabaseTemplatesPrefix = "database/templates/";

        private static readonly ResourceType[] s_onlyTemplateResourceTypes = new ResourceType[] { ResourceType.Template };

        private readonly bool _lazyBundleCreation = false;

        public TemplatesResourceBundleDiscoverer(bool lazyBundleCreation)
        {
            _lazyBundleCreation = lazyBundleCreation;
        }

        /// <summary>
        /// Create a metadata provider from given path.
        /// </summary>
        public ResourceBundleDiscoveryResult Discover(string path)
        {
            if (IO.Directory.Exists(path))
            {
                var pathInfo = new IO.DirectoryInfo(path);
                if (pathInfo.Name == "database")
                {
                    var result = CreateFromDatabaseDirectoryOrNull(path);
                    if (result != null) return result.Value;
                    throw MetadataNotFound(path);
                }

                if (pathInfo.Name == "templates")
                {
                    return CreateFromDirectory(path, "database/templates/");
                }

                var databasePath = JoinPathAndReturnIfDirectoryExist(path, "database");
                if (databasePath != null)
                {
                    var result = CreateFromDatabaseDirectoryOrNull(databasePath);
                    if (result != null) return result.Value;
                    // There is expected case, when there is no metadata in this
                    // directory.
                }

                var templatesPath = JoinPathAndReturnIfDirectoryExist(path, "templates");
                if (templatesPath != null)
                {
                    return CreateFromDirectory(templatesPath, "database/templates/");
                }

                var archivePath = JoinPathAndReturnIfFileExist(path, "Toolset/Templates.arc");
                if (archivePath != null)
                {
                    return CreateFromArchive(archivePath, prefix: "database/");
                }

                throw MetadataNotFound(path);
            }

            // File exist, it might be .ARC or .ZIP file.
            if (IO.File.Exists(path))
            {
                return CreateFromArchive(path, prefix: null);
            }

            throw MetadataNotFound(path);
        }

        private ResourceBundleDiscoveryResult? CreateFromDatabaseDirectoryOrNull(string databasePath)
        {
            string? templatesPath = JoinPathAndReturnIfDirectoryExist(databasePath, "templates");
            if (templatesPath != null)
            {
                return CreateFromDirectory(templatesPath, "database/templates/");
            }

            string? archivePath = JoinPathAndReturnIfFileExist(databasePath, "templates.arc");
            if (archivePath != null)
            {
                return CreateFromArchive(archivePath, prefix: "database/templates/");
            }

            return null;
        }

        private ResourceBundleDiscoveryResult CreateFromDirectory(string path, string? prefix)
        {
            Check.Argument.NotNull(prefix, nameof(prefix));

            return CreateBundleDiscoveryResult(() =>
                new FileSystemBundle(
                    name: GetMetadataBundleName(),
                    path,
                    supportedResourceTypes: s_onlyTemplateResourceTypes
                    ), prefix, null);
        }

        private ResourceBundleDiscoveryResult CreateFromArchive(string path, string? prefix)
        {
            if (path.EndsWith(".arc", StringComparison.OrdinalIgnoreCase))
            {
                var archive = ArcArchive.Open(path, ArcArchiveMode.Read);

                if (prefix == null)
                {
                    prefix = DetectPrefix(archive.SelectAll().Select(x => x.Name));
                }

                return CreateBundleDiscoveryResult(() => new ArcArchiveBundle(
                    name: GetMetadataBundleName(),
                    physicalPath: path,
                    supportedResourceTypes: s_onlyTemplateResourceTypes,
                    archive: archive,
                    disposeArchive: true), prefix, externalPrefix: null);
            }
            else if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var archive = ZipFile.OpenRead(path);

                if (prefix == null)
                {
                    prefix = DetectPrefix(archive.Entries.Select(x => x.FullName));
                }

                return CreateBundleDiscoveryResult(() => new ZipArchiveBundle(
                    name: GetMetadataBundleName(),
                    physicalPath: path,
                    supportedResourceTypes: s_onlyTemplateResourceTypes,
                    archive: archive,
                    disposeArchive: true), prefix, externalPrefix: null);
            }
            else throw InvalidMetadataPath(path);
        }

        private ResourceBundleDiscoveryResult CreateBundleDiscoveryResult(
            Func<ResourceBundle> factory, string? prefix, string? externalPrefix)
        {
            if (_lazyBundleCreation) return new ResourceBundleDiscoveryResult(factory, prefix, externalPrefix);
            else
            {
                var bundle = factory();
                return new ResourceBundleDiscoveryResult(bundle, prefix, externalPrefix);
            }
        }

        private string DetectPrefix(IEnumerable<string> fileNames)
        {
            string? bundlePrefix = null;

            foreach (var x in fileNames)
            {
                if (Path.Implicit(x).EndsWith("actor.tpl", PathComparison.OrdinalIgnoreCase))
                {
                    var prefix = Path.Implicit(IO.Path.GetDirectoryName(x));

                    string? detectedPrefix;
                    if (prefix.Equals("database/templates", PathComparison.OrdinalIgnoreCase))
                    {
                        detectedPrefix = "";
                    }
                    else if (prefix.Equals("templates", PathComparison.OrdinalIgnoreCase))
                    {
                        detectedPrefix = "database";
                    }
                    else if (prefix.Equals("", PathComparison.OrdinalIgnoreCase))
                    {
                        detectedPrefix = "database/templates";
                    }
                    else detectedPrefix = null;

                    if (detectedPrefix != null)
                    {
                        if (bundlePrefix == null
                            || detectedPrefix.Length < bundlePrefix.Length)
                        {
                            bundlePrefix = detectedPrefix;
                            break;
                        }
                    }
                }
            }

            if (bundlePrefix == null) throw Error.InvalidOperation("Unable to detect bundle prefix. Given templates archive ambigious.");

            return bundlePrefix;
        }

        private static string? JoinPathAndReturnIfDirectoryExist(string path1, string path2)
        {
            var joined = IO.Path.Join(path1, path2);
            if (IO.Directory.Exists(joined))
            {
                return joined;
            }
            else
            {
                return null;
            }
        }

        private static string? JoinPathAndReturnIfFileExist(string path1, string path2)
        {
            var joined = IO.Path.Join(path1, path2);
            if (IO.File.Exists(joined))
            {
                return joined;
            }
            else
            {
                return null;
            }
        }

        private static Exception MetadataNotFound(string path)
        {
            // TODO: Throw right diagnostics
            return Error.InvalidOperation("Metadata not found on given path: \"{0}\".", path);
        }

        private static Exception InvalidMetadataPath(string path)
        {
            // TODO: Throw right diagnostics
            return Error.InvalidOperation("Invalid metadata path: \"{0}\".", path);
        }

        private static string GetMetadataBundleName()
        {
            return "metadata";
        }
    }
}
