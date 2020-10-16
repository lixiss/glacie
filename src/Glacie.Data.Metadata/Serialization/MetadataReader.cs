using System;
using System.Xml.Linq;

using Glacie.Metadata.Builder;

using IO = System.IO;

namespace Glacie.Metadata.Serialization
{
    // TODO: Consider to rename it to indicate what it is XML-based, while
    // XmlMetadataReader rename to indicate what it is internal implementation.
    public sealed class MetadataReader
    {
        private readonly MetadataBuilder _metadataBuilder;
        private readonly Func<string, IO.Stream?>? _resourceResolver;

        public MetadataReader(MetadataBuilder metadataBuilder, MetadataReaderOptions? options = null)
        {
            _metadataBuilder = metadataBuilder;
            _resourceResolver = options?.ResourceResolver;
        }

        /// <summary>
        /// Read metadata from given path.
        /// If custom resource resolver is passed, then it will be used.
        /// </summary>
        public void Read(string path)
        {
            using var stream = ResolveStream(path);
            ReadInternal(stream, path);
        }

        /// <summary>
        /// Read metadata from stream, with associated path (filename)
        /// (which will be used for reporting errors, and as base path for
        /// resolving external resource references).
        /// </summary>
        public void Read(IO.Stream stream, string path)
        {
            ReadInternal(stream, path);
        }

        /// <summary>
        /// Read metadata from stream, with associated path (filename)
        /// (which will be used for reporting errors, and as base path for
        /// resolving external resource references).
        /// </summary>
        private void ReadInternal(IO.Stream stream, string path)
        {
            Check.Argument.NotNull(path, nameof(path));

            var basePath = IO.Path.GetDirectoryName(path);

            Func<string, GxmDocumentType, bool>? processIncludeDirective = null;
            if (basePath != null)
            {
                processIncludeDirective = (includePath, parentDocumentType) =>
                    ProcessIncludeDirective(includePath, basePath, parentDocumentType);
            }

            var document = ParseStream(stream);
            CreateReader(path, document, processIncludeDirective, parentDocumentType: GxmDocumentType.Unknown)
                .Read();
        }

        private IO.Stream? ResolveStreamOrNull(string path)
        {
            if (_resourceResolver != null)
            {
                return _resourceResolver(path);
            }
            else
            {
                if (IO.File.Exists(path)) return IO.File.OpenRead(path);
                else return null;
            }
        }

        private IO.Stream ResolveStream(string path)
        {
            var result = ResolveStreamOrNull(path);
            if (result == null)
            {
                throw Error.InvalidOperation("Metadata reader can't resolve resource stream for path: \"{0}\".", path);
            }
            return result;
        }

        private bool ProcessIncludeDirective(string includePath, string basePath, GxmDocumentType parentDocumentType)
        {
            var actualPath = IO.Path.Combine(basePath, includePath);

            var stream = ResolveStreamOrNull(actualPath);
            if (stream == null) return false;

            var document = ParseStream(stream);

            CreateReader(actualPath, document, processIncludeDirective: null, parentDocumentType: parentDocumentType)
                .Read();

            return true;
        }

        private XDocument ParseStream(IO.Stream stream)
        {
            var document = XDocument.Load(stream, LoadOptions.SetLineInfo);
            return document;
        }

        private XmlMetadataReader CreateReader(string path, XDocument document,
            Func<string, GxmDocumentType, bool>? processIncludeDirective,
            GxmDocumentType parentDocumentType)
        {
            // TODO: pass result collector
            return new XmlMetadataReader(_metadataBuilder, path, document, processIncludeDirective, parentDocumentType);
        }
    }
}
