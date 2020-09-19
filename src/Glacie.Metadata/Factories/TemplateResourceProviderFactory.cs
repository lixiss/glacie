using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

using Glacie.Abstractions;
using Glacie.Data.Arc;
using Glacie.Resources.Providers;

using IO = System.IO;

namespace Glacie.Metadata
{
    public static class TemplateResourceProviderFactory
    {
        private static readonly ResourceType[] s_supportedResourceTypes = new ResourceType[] { ResourceType.Template };

        private static readonly VirtualPath s_databaseTemplatesSegment = new VirtualPath("database/templates").Normalize(VirtualPathNormalization.Standard);
        private static readonly VirtualPath s_databaseSegment = new VirtualPath("database").Normalize(VirtualPathNormalization.Standard);
        private static readonly VirtualPath s_templatesSegment = new VirtualPath("templates").Normalize(VirtualPathNormalization.Standard);

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

                    return new FileSystemResourceProvider(path,
                        supportedResourceTypes: s_supportedResourceTypes,
                        virtualBasePath: "database");
                }
                else if (directoryName.Equals("templates", StringComparison.OrdinalIgnoreCase))
                {
                    return new FileSystemResourceProvider(path,
                        supportedResourceTypes: s_supportedResourceTypes,
                        virtualBasePath: "database/templates");
                }
                else if (directoryName.Equals("working", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: should it look in working/database/templates directory only?

                    return new FileSystemResourceProvider(path,
                        supportedResourceTypes: s_supportedResourceTypes,
                        virtualBasePath: null);
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

                    return new FileSystemResourceProvider(path, 
                        supportedResourceTypes: s_supportedResourceTypes,
                        virtualBasePath: null);
                }
            }
            else
            {
                // TODO: Generate proper error.
                throw Error.InvalidOperation("Can't create template resolver for path \"{0}\".");
            }
        }

        public static ResourceProvider Create(ArcArchive archive, in VirtualPath? virtualRoot, bool keepArchiveOpen = false)
        {
            return new ArcArchiveResourceProvider(archive,
                supportedResourceTypes: s_supportedResourceTypes,
                virtualRoot: virtualRoot ?? DetectArchiveVirtualRoot(archive.GetEntries().Select(x => x.Name)),
                keepArchiveOpen: keepArchiveOpen);
        }

        public static ResourceProvider Create(ZipArchive archive, in VirtualPath? virtualRoot, bool keepArchiveOpen = false)
        {
            return new ZipArchiveResourceProvider(archive,
                supportedResourceTypes: s_supportedResourceTypes,
                virtualRoot: virtualRoot ?? DetectArchiveVirtualRoot(archive.Entries.Select(x => x.FullName)),
                keepArchiveOpen: keepArchiveOpen);
        }

        private static ResourceProvider CreateFromArcArchive(string path)
        {
            ResourceProvider? result = null;
            var archive = ArcArchive.Open(path, ArcArchiveMode.Read);
            try
            {
                return (result = Create(archive, virtualRoot: null, keepArchiveOpen: false));
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
                return result = Create(archive, virtualRoot: null, keepArchiveOpen: false);
            }
            finally
            {
                if (result == null) archive.Dispose();
            }
        }

        private static VirtualPath DetectArchiveVirtualRoot(IEnumerable<string> entryNames)
        {
            var limit = 10;

            foreach (var entryName in entryNames)
            {
                VirtualPath vp = entryName;
                if (vp.StartsWithSegment(s_databaseSegment, VirtualPathComparison.NonStandard))
                    return ""; // TODO: return null. VirtualPath should work with nulls.
                else if (vp.StartsWithSegment(s_templatesSegment, VirtualPathComparison.NonStandard))
                    return s_databaseSegment;

                limit--;
                if (limit <= 0) break;
            }
            return s_databaseTemplatesSegment;
        }
    }
}
