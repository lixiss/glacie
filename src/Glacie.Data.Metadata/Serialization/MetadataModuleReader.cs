using System.Collections.Generic;
using System.Xml.Linq;

using Glacie.Diagnostics;

using IO = System.IO;

namespace Glacie.Metadata.Serialization
{
    public sealed class MetadataModuleReader
    {
        public MetadataModuleReader() { }

        public List<string> Read(IO.Stream stream, string path)
        {
            return ReadInternal(stream, path);
        }

        private List<string> ReadInternal(IO.Stream stream, string path)
        {
            Check.Argument.NotNull(path, nameof(path));

            var basePath = IO.Path.GetDirectoryName(path);
            Check.That(basePath != null);

            var document = ParseStream(stream);
            var context = new Context(path, basePath, document);
            return context.Read();
        }

        private ref struct Context
        {
            private string _path;
            private string _basePath;
            private XDocument _document;

            public Context(string path, string basePath, XDocument document)
            {
                _path = path;
                _basePath = basePath;
                _document = document;
            }

            public List<string> Read()
            {
                var rootElement = _document.Root;

                switch (rootElement.Name.LocalName)
                {
                    case Naming.MetadataModuleRootElement:
                        return ReadRoot(rootElement);

                    default:
                        throw DiagnosticFactory
                        .ReaderError(GetLocation(rootElement), "Unknown document type.")
                        .AsException();
                }
            }

            private List<string> ReadRoot(XElement element)
            {
                DebugCheck.That(element.Name.LocalName == Naming.MetadataModuleRootElement);

                foreach (var a in element.Attributes())
                {
                    switch (a.Name.LocalName)
                    {
                        case "xmlns": continue;
                        default:
                            throw DiagnosticFactory
                                .UnexpectedAttribute(GetLocation(a), a)
                                .AsException();
                    }
                }

                var result = new List<string>();
                foreach (var e in element.Elements())
                {
                    switch (e.Name.LocalName)
                    {
                        case Naming.IncludeElement:
                            {
                                var path = ReadIncludeElement(e);
                                var actualPath = Path
                                    .Join(_basePath, path)
                                    .Convert(PathConversions.Normalized | PathConversions.DirectorySeparator, check: true);
                                result.Add(actualPath.ToString());
                            }
                            break;

                        default:
                            throw DiagnosticFactory
                                .UnexpectedElement(GetLocation(e), e)
                                .AsException();
                    }
                }
                return result;
            }

            private string ReadIncludeElement(XElement element)
            {
                DebugCheck.That(element.Name.LocalName == Naming.IncludeElement);

                string? includePath = null;

                foreach (var a in element.Attributes())
                {
                    switch (a.Name.LocalName)
                    {
                        case Naming.IncludePathAttribute:
                            ReadStringNotEmpty(a, ref includePath);
                            break;

                        default:
                            throw DiagnosticFactory
                                .UnexpectedAttribute(GetLocation(a), a)
                                .AsException();
                    }
                }

                EnsureRequiredAttribute(ref includePath, element, Naming.IncludePathAttribute);
                Check.That(includePath != null);

                return includePath;
            }

            private void ReadStringNotEmpty(XAttribute attribute, ref string? value)
            {
                var attributeValue = attribute.Value;
                Check.That(attributeValue != null);
                Check.That(value == null);
                if (string.IsNullOrWhiteSpace(attributeValue))
                {
                    throw DiagnosticFactory
                        .AttributeCannotBeEmpty(GetLocation(attribute), attribute)
                        .AsException();
                }
                value = attributeValue.Trim();
            }

            private void EnsureRequiredAttribute(ref string? value, XElement element, string attributeName)
            {
                if (value == null)
                {
                    throw DiagnosticFactory
                        .ElementMustHaveAttribute(GetLocation(element), element, attributeName)
                        .AsException();
                }
            }

            private Location GetLocation(System.Xml.IXmlLineInfo xmlLineInfo)
            {
                if (xmlLineInfo.HasLineInfo())
                {
                    return Location.File(_path, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                }
                else if (_path != null)
                {
                    return Location.JustFile(_path);
                }
                else return Location.None;
            }
        }


        private XDocument ParseStream(IO.Stream stream)
        {
            var document = XDocument.Load(stream, LoadOptions.SetLineInfo);
            return document;
        }

        private static class Naming
        {
            public const string MetadataModuleRootElement = "metadata-module";
            public const string IncludeElement = "include";
            public const string IncludePathAttribute = "path";
        }
    }
}
