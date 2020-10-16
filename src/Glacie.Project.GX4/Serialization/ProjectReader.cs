using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using Glacie.Diagnostics;
using Glacie.ProjectSystem.Builder;
using Glacie.ProjectSystem.Serialization.Model;

using IO = System.IO;

namespace Glacie.ProjectSystem.Serialization
{
    using Naming = XmlProjectNames;

    /// <summary>
    /// Read project file from given file or stream,
    /// and returns <see cref="ProjectBuilder"/> instance.
    /// </summary>
    public sealed class ProjectReader
    {
        #region API

        public static ProjectBuilder Read(string path)
        {
            var physicalPath = IO.Path.GetFullPath(path);
            using var stream = IO.File.OpenRead(physicalPath);
            return Read(stream, physicalPath);
        }

        public static ProjectBuilder Read(IO.Stream stream)
            => Read(stream, physicalPath: null);

        public static ProjectBuilder Read(IO.Stream stream, string? physicalPath)
        {
            // TODO: This might throw some exception, for example on duplicated attribute.
            var document = XDocument.Load(stream, LoadOptions.SetLineInfo);
            return new ProjectReader(physicalPath).Read(document);
        }

        #endregion

        private readonly ProjectBuilder _projectBuilder;
        private readonly string? _physicalPath;

        private ProjectReader(string? physicalPath)
        {
            _projectBuilder = new ProjectBuilder();
            _physicalPath = physicalPath;
        }

        private ProjectBuilder Read(XDocument document)
        {
            var model = ReadModel(document);

            if (model.Sources != null)
            {
                foreach (var source in model.Sources)
                {
                    _projectBuilder.AddSource(source.Path);
                }
            }

            if (model.Metadata != null)
            {
                _projectBuilder.WithMetadata(model.Metadata.Path);
            }

            return _projectBuilder;
        }

        private ProjectModel ReadModel(XDocument document)
        {
            var rootElement = document.Root;

            if (rootElement.Name.LocalName != Naming.RootElementName)
            {
                throw DiagnosticFactory
                    .UnexpectedElement(GetLocation(rootElement), rootElement)
                    .AsException();
            }

            return ReadRoot(rootElement);
        }

        private ProjectModel ReadRoot(XElement element)
        {
            DebugCheck.That(element.Name.LocalName == Naming.RootElementName);

            // project model's properties
            var sources = new List<ProjectSourceModel>();
            ProjectMetadataModel? metadata = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case "xmlns":
                        {
                            var rootNamespace = a.Value;
                            if (!(rootNamespace == Naming.NamespaceName || rootNamespace == ""))
                            {
                                throw DiagnosticFactory
                                    .UnexpectedAttribute(GetLocation(a), a)
                                    .AsException();
                            }
                        }
                        continue;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    case Naming.SourceElementName:
                        var source = ReadSource(e);
                        sources.Add(source);
                        break;

                    case Naming.MetadataElementName:
                        if (metadata != null)
                        {
                            throw DiagnosticFactory
                                .ElementMustBeUsedOnlyOnce(GetLocation(e), e)
                                .AsException();
                        }
                        metadata = ReadMetadata(e);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }

            return new ProjectModel
            {
                Sources = sources,
                Metadata = metadata,
            };
        }

        private ProjectSourceModel ReadSource(XElement element)
        {
            DebugCheck.That(element.Name.LocalName == Naming.SourceElementName);

            string? path = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.SourcePathAttributeName:
                        path = a.Value;
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }

            return new ProjectSourceModel { Path = path };
        }

        private ProjectMetadataModel ReadMetadata(XElement element)
        {
            DebugCheck.That(element.Name.LocalName == Naming.MetadataElementName);

            string? path = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.MetadataPathAttributeName:
                        path = a.Value;
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }

            return new ProjectMetadataModel { Path = path };
        }

        private Location GetLocation(IXmlLineInfo xmlLineInfo)
        {
            if (xmlLineInfo.HasLineInfo())
            {
                return Location.File(_physicalPath, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
            }
            else if (_physicalPath != null)
            {
                return Location.JustFile(_physicalPath);
            }
            else return Location.None;
        }
    }
}
