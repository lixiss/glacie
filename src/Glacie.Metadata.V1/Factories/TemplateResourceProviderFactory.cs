using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

using Glacie.Data.Arc;
using Glacie.Data.Resources.V1;
using Glacie.Data.Resources.V1.Providers;

using IO = System.IO;

namespace Glacie.Metadata.V1
{
    public static class TemplateResourceProviderFactory
    {
        private static readonly ResourceType[] s_supportedResourceTypes = new ResourceType[] { ResourceType.Template };

        private static readonly Path1 s_databaseTemplatesSegment = new Path1("database/templates").ToForm(Constants.TemplatePathForm);
        private static readonly Path1 s_databaseSegment = new Path1("database").ToForm(Constants.TemplatePathForm);
        private static readonly Path1 s_templatesSegment = new Path1("templates").ToForm(Constants.TemplatePathForm);

        public static ResourceProvider Create(string path)
        {
            if (IO.File.Exists(path))
            {
                if (path.EndsWith(".arc", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateFromArcArchive(path);
                }
                else if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateFromZipArchive(path);
                }
                else
                {
                    // TODO: Generate proper error.
                    throw Error.InvalidOperation("Can't create template resolver - unknown file.");
                }
            }
            else if (IO.Directory.Exists(path))
            {
                // path might point to:
                //   ./working directory (then no need to do any adjustments, however prefer to enumerate database/templates/* if need read all templates)
                //   ./database directory
                //   ./templates directory
                // also path might point to game directory:
                //   <game>/database/templates.arc
                //   <game>/Toolset/Templates.arc

                var directoryName = IO.Path.GetFileName(path.AsSpan());
                if (directoryName.Equals("database", StringComparison.OrdinalIgnoreCase))
                {
                    var archivePath = IO.Path.Combine(path, "templates.arc");
                    if (IO.File.Exists(archivePath))
                    {
                        return CreateFromArcArchive(archivePath);
                    }

                    return new FileSystemResourceProvider(
                        name: "<file-system>(" + path + ")",
                        virtualBasePath: Path1.From("database"),
                        virtualPathForm: Constants.TemplatePathForm,
                        internalBasePath: default,
                        internalPathForm: Path1Form.Any,
                        supportedTypes: s_supportedResourceTypes,
                        basePath: path);
                }
                else if (directoryName.Equals("templates", StringComparison.OrdinalIgnoreCase))
                {
                    return new FileSystemResourceProvider(
                        name: "<file-system>(" + path + ")",
                        virtualBasePath: Path1.From("database/templates"),
                        virtualPathForm: Constants.TemplatePathForm,
                        internalBasePath: default,
                        internalPathForm: Path1Form.Any,
                        supportedTypes: s_supportedResourceTypes,
                        basePath: path);
                }
                else if (directoryName.Equals("working", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: should it look in working/database/templates directory only?

                    return new FileSystemResourceProvider(
                        name: "<file-system>(" + path + ")",
                        virtualBasePath: default,
                        virtualPathForm: Constants.TemplatePathForm,
                        internalBasePath: default,
                        internalPathForm: Path1Form.Any,
                        supportedTypes: s_supportedResourceTypes,
                        basePath: path);
                }
                else
                {
                    // Grim Dawn
                    var archivePath = IO.Path.Combine(path, "database/templates.arc");
                    if (IO.File.Exists(archivePath))
                    {
                        return CreateFromArcArchive(archivePath);
                    }

                    // Titan Quest Anniversary Edition
                    archivePath = IO.Path.Combine(path, "Toolset/Templates.arc");
                    if (IO.File.Exists(archivePath))
                    {
                        return CreateFromArcArchive(archivePath);
                    }

                    return new FileSystemResourceProvider(
                        name: "<file-system>(" + path + ")",
                        virtualBasePath: default,
                        virtualPathForm: Constants.TemplatePathForm,
                        internalBasePath: default,
                        internalPathForm: Path1Form.Any,
                        supportedTypes: s_supportedResourceTypes,
                        basePath: path);
                }
            }
            else
            {
                // TODO: Generate proper error.
                throw Error.InvalidOperation("Can't create template resolver for path \"{0}\".");
            }
        }

        public static ResourceProvider Create(string name, ArcArchive archive, in Path1? virtualRoot,
            Path1Form virtualPathNormalization = Constants.TemplatePathForm,
            bool keepArchiveOpen = false)
        {
            return new ArcArchiveResourceProvider(
                name: name,
                virtualBasePath: virtualRoot ?? DetectArchiveVirtualRoot(archive.SelectAll().Select(x => x.Name)),
                virtualPathForm: virtualPathNormalization,
                internalBasePath: default,
                internalPathForm: Path1Form.Any,
                supportedTypes: s_supportedResourceTypes,
                archive: archive,
                disposeArchive: !keepArchiveOpen);
        }

        public static ResourceProvider Create(string name, ZipArchive archive, in Path1? virtualRoot,
            Path1Form virtualPathNormalization = Constants.TemplatePathForm,
            bool keepArchiveOpen = false)
        {
            // TODO: support normalization or mappers in all providers

            return new ZipArchiveResourceProvider(
                name: name,
                virtualBasePath: virtualRoot ?? DetectArchiveVirtualRoot(archive.Entries.Select(x => x.FullName)),
                virtualPathForm: virtualPathNormalization,
                internalBasePath: default,
                internalPathForm: Path1Form.Any,
                supportedTypes: s_supportedResourceTypes,
                archive: archive,
                disposeArchive: !keepArchiveOpen);
        }

        private static ResourceProvider CreateFromArcArchive(string path)
        {
            ResourceProvider? result = null;
            var archive = ArcArchive.Open(path, ArcArchiveMode.Read);
            try
            {
                return (result = Create(name: "<arc>(" + path + ")",
                    archive, virtualRoot: null, keepArchiveOpen: false));
            }
            finally
            {
                if (result == null) archive.Dispose();
            }
        }

        private static ResourceProvider CreateFromZipArchive(string path)
        {
            ResourceProvider? result = null;
            var archive = ZipFile.OpenRead(path);
            try
            {
                return result = Create(name: "<zip>(" + path + ")",
                    archive, virtualRoot: null, keepArchiveOpen: false);
            }
            finally
            {
                if (result == null) archive.Dispose();
            }
        }

        private static Path1 DetectArchiveVirtualRoot(IEnumerable<string> entryNames)
        {
            var limit = 10;

            foreach (var entryName in entryNames)
            {
                var vp = Path1.From(entryName);
                if (vp.StartsWith(s_databaseSegment, Path1Comparison.OrdinalIgnoreCase))
                    return default;
                else if (vp.StartsWith(s_templatesSegment, Path1Comparison.OrdinalIgnoreCase))
                    return s_databaseSegment;

                limit--;
                if (limit <= 0) break;
            }
            return s_databaseTemplatesSegment;
        }
    }
}
