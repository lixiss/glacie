using System;
using System.Diagnostics.Tracing;
using System.Xml.Linq;

using Glacie.Metadata.Builder;

using IO = System.IO;

namespace Glacie.Metadata.Serialization
{
    // TODO: Reader should able to report diagnostics, so current API is very bad.
    // TODO: Usage
    // new XmlMetadataReader(options... include diagnostics).Read(readStream).
    public sealed class __XmlMetadataReader
    {
        private readonly MetadataBuilder _databaseType;
        private readonly bool _patchMode;
        private readonly Func<string, IO.Stream>? _resourceResolver;

        private readonly __ElementNames _names;

        public __XmlMetadataReader(MetadataBuilder databaseType,
            bool patch = false,
            Func<string, IO.Stream>? resourceResolver = null)
        {
            _databaseType = databaseType;
            _patchMode = patch;
            _resourceResolver = resourceResolver;

            _names = new __ElementNames(patch);
        }

        public void Read(string path)
        {
            using var stream = ResolveResource(path);

            var x = IO.Path.GetDirectoryName(path);

            ReadInternal(stream, path, x);
        }

        public void Read(IO.Stream resourceStream, string path)
        {
            ReadInternal(resourceStream, path, IO.Path.GetDirectoryName(path));
            // throw Error.NotImplemented();
        }

        public void Read(IO.Stream resourceStream)
        {
            ReadInternal(resourceStream, "", "");
        }

        private void ReadInternal(IO.Stream stream, string path, string basePath)
        {
            Console.WriteLine("Reading: {0} Base Path: {1}", path, basePath);

            var context = new Context { Path = path, BasePath = basePath };

            var document = XDocument.Load(stream, LoadOptions.SetLineInfo);
            if (document.Root.Name == _names.MetadataRootElementName)
            {
                ReadMetadataDocument(ref context, document, isIncludeDocument: false);
            }
            else if (document.Root.Name == _names.MetadataIncludeRootElementName)
            {
                ReadMetadataDocument(ref context, document, isIncludeDocument: true);
            }
            else throw Error.InvalidOperation("Invalid document type.");
        }

        private void ReadMetadataDocument(ref Context context, XDocument document, bool isIncludeDocument)
        {
            // TODO: Split to RootElement, and enforce attribute checks.
            // Presence of invalid attributes is error.

            foreach (var e in document.Root.Elements())
            {
                if (e.Name == _names.RecordTypeElementName)
                {
                    ReadRecordType(ref context, e);
                    // Console.WriteLine(e.Name);
                    // throw Error.NotImplemented();
                }
                else if (e.Name == _names.IncludeElementName)
                {
                    if (!isIncludeDocument)
                    {
                        ReadInclude(ref context, e);
                    }
                    else
                    {
                        throw Error.InvalidOperation("Include elements may appear only at main document.");
                    }
                }
                else throw Error.InvalidOperation("Unexpected element {0}.", e.Name);
            }
        }

        private void ReadRecordType(ref Context context, XElement element)
        {
            DebugCheck.That(element.Name == _names.RecordTypeElementName);

            var recordTypeName = GetRequiredAttributeString(element, _names.NameAttributeName);
            if (!_databaseType.TryGetRecordType(recordTypeName, out var recordType))
            {
                recordType = _databaseType.DefineRecordType(recordTypeName);
                // read new
            }
            else
            {
                throw Error.InvalidOperation("RecordType already defined."); // location
            }

            // ...process...
        }

        private string GetRequiredAttributeString(XElement element, XName attributeName)
        {
            var attribute = element.Attribute(attributeName);
            if (attribute == null) throw Error.InvalidOperation("Missing required attribute {0}.", attributeName);
            var value = attribute.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Error.InvalidOperation("Attribute {0} of element {1} must not be empty.", attributeName, element);
            }
            return value.Trim();
        }

        private void ReadInclude(ref Context context, XElement element)
        {
            DebugCheck.That(element.Name == _names.IncludeElementName);

            var pathAttribute = element.Attribute(_names.IncludePathAttributeName);
            if (pathAttribute == null) throw Error.InvalidOperation("Include element missing path attribute.");

            var includePath = pathAttribute.Value;
            if (string.IsNullOrEmpty(includePath))
            {
                throw Error.InvalidOperation("Include path attribute must not be empty.");
            }

            var incPath = Path1.From(includePath).ToForm(__XmlMetadataWriter.IncludePathForm);
            if (!incPath.IsInForm(__XmlMetadataWriter.IncludePathForm))
            {
                throw Error.InvalidOperation("Include path is invalid format.");
            }

            var includeResourcePath = Path1.Join(context.BasePath, includePath);

            var includedStream = ResolveResource(includeResourcePath.ToString());
            ReadInternal(includedStream,
                includeResourcePath.ToString(),
                IO.Path.GetDirectoryName(includeResourcePath.ToString()));
        }


        private struct Context
        {
            public string Path;
            public string BasePath;
        }

        private IO.Stream ResolveResource(string path)
        {
            if (_resourceResolver == null)
            {
                throw Error.InvalidOperation("No resource resolver provided.");
            }

            var stream = _resourceResolver(path);
            if (stream == null)
            {
                throw Error.InvalidOperation("Resource resolver doesn't return resource for path: {0}.", path.ToString());
            }

            return stream;
        }
    }
}
