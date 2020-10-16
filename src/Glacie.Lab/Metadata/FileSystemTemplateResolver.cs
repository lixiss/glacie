using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Glacie.Data.Templates;

namespace Glacie.Lab.Metadata
{
    // Hacky file system template resolver.
    // TODO: Create TemplateResolver abstract class which may do caching?
    internal sealed class FileSystemTemplateResolver // : ITemplateResolver
    {
        private const string TemplateDirPrefix = "%TEMPLATE_DIR%";

        private readonly string _templatesDir;

        private readonly TemplateReader _templateReader;

        public FileSystemTemplateResolver(string templatesDir)
        {
            _templatesDir = templatesDir;
            _templateReader = new TemplateReader();
        }

        public Template ResolveTemplate(string name)
        {
            name = TrimStartPathSegment(name, TemplateDirPrefix);

            // TODO: This is hack.
            name = TrimStartPathSegment(name, "database");
            name = TrimStartPathSegment(name, "templates");


            // Normalize
            name = name.ToLowerInvariant().Replace('\\', '/');

            var templatePath = System.IO.Path.Combine(_templatesDir, name);

            var template = _templateReader.Read(Path1.From(templatePath));
            return template;
        }

        private static string TrimStartPathSegment(string path, string segment)
        {
            if (path.StartsWith(segment, StringComparison.OrdinalIgnoreCase))
            {
                var segmentLength = segment.Length;
                if (path.Length > segmentLength)
                {
                    if (path[segmentLength] == '/' || path[segmentLength] == '\\')
                    {
                        // TODO: should trim multiple occurencies of '/' or '\\'
                        return path.Substring(segmentLength + 1);
                    }
                    else
                    {
                        return path.Substring(segmentLength);
                    }
                }
            }
            return path;
        }
    }
}
