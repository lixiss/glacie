using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using IO = System.IO;

namespace Glacie.Metadata.Serialization
{
    public sealed class MetadataWriter
    {
        private const PathConversions IncludePathForm
            = PathConversions.Relative
            | PathConversions.Strict
            | PathConversions.Normalized
            | PathConversions.DirectorySeparator;

        private readonly DatabaseType _databaseType;

        private readonly bool _emitPatchBoilerplate;
        private readonly bool _omitXmlDeclaration;
        private readonly bool _useXmlNamespace;
        private readonly bool _emitRootVarGroup;
        private readonly bool _emitVarGroups;
        private readonly bool _includeOnlyVarProperties;
        private readonly bool _excludeVarProperties; // todo: _excludeVarProperties not implemented
        private readonly bool _emitVarPropertyAsAttr;

        private bool EmitRecordPath => !_emitPatchBoilerplate;
        private bool EmitInherits => !_emitPatchBoilerplate;
        private bool EmitExpressionVariables => !_emitPatchBoilerplate;
        private bool EmitProperties => !_emitPatchBoilerplate;
        private bool EmitDescription => !_emitPatchBoilerplate;

        private readonly XElementNames _naming;

        public MetadataWriter(DatabaseType databaseType, MetadataWriterOptions? options = null)
        {
            _databaseType = databaseType;

            _emitPatchBoilerplate = options?.EmitPatchBoilerplate ?? false;
            _omitXmlDeclaration = options?.OmitXmlDeclaration ?? true;
            _useXmlNamespace = options?.UseXmlNamespace ?? false;
            _emitRootVarGroup = options?.EmitRootVarGroup ?? true;
            _emitVarGroups = options?.EmitVarGroups ?? true;
            _includeOnlyVarProperties = options?.IncludeOnlyVarProperties ?? false;
            _excludeVarProperties = options?.ExcludeVarProperties ?? false;
            _emitVarPropertyAsAttr = options?.EmitVarPropertyAsAttribute ?? true;

            var @namespace = _useXmlNamespace ? XElementNames.MetadataNamespace : XNamespace.None;

            _naming = new XElementNames(@namespace);
        }

        public void Write(IO.Stream stream)
        {
            WriteInternal((x) => stream, multipart: false);
        }

        /// <summary>
        /// Write multipart metadata.
        /// <example>
        /// Sample:
        /// <code>
        /// metadataWriter.Write((path) =>
        /// {
        ///     var dir = IO.Path.GetDirectoryName(path);
        ///     if (!string.IsNullOrEmpty(dir)) IO.Directory.CreateDirectory(dir);
        ///     return IO.File.Create(path);
        /// });
        /// </code>
        /// </example>
        /// </summary>
        public void Write(Func<string, IO.Stream> outputStreamProvider,
            string? mainFileName = null,
            string? includeSubDirectory = null)
        {
            WriteInternal(outputStreamProvider, multipart: true,
                mainFileName: mainFileName,
                includeSubDirectory: includeSubDirectory);
        }

        private void WriteInternal(Func<string, IO.Stream> outputStreamProvider, bool multipart,
            string? mainFileName = null,
            string? includeSubDirectory = null)
        {
            if (!multipart)
            {
                var document = CreateMetadataDocument();
                using var outputStream = outputStreamProvider("");
                SaveDocument(document, outputStream);
                outputStream.Dispose();
            }
            else
            {
                WriteMultipart(outputStreamProvider, mainFileName, includeSubDirectory);
            }
        }

        private void WriteMultipart(
            Func<string, IO.Stream> outputStreamProvider,
            string? mainFileName = null, string? includeSubDirectory = null)
        {
            var relativePath = ".";
            if (string.IsNullOrEmpty(mainFileName)) mainFileName = "!metadata";
            var mainExtension = _emitPatchBoilerplate ? ".gxmp" : ".gxmd";
            var includeExtension = _emitPatchBoilerplate ? ".gxmpi" : ".gxmdi";
            if (!Path.GetExtension(mainFileName).Equals(mainExtension, StringComparison.OrdinalIgnoreCase))
            {
                mainFileName += mainExtension;
            }

            var outputItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var recordType in GetSortedRecordTypes(_databaseType))
            {
                var recordDocument = CreateMetadataIncludeDocument(recordType);

                string outputPathRaw = recordType.Name
                        .Replace('.', '/')
                        .Replace(' ', '_');
                if (string.IsNullOrEmpty(includeSubDirectory))
                {
                    outputPathRaw = relativePath + "/" + outputPathRaw;
                }
                else
                {
                    outputPathRaw = relativePath + "/" + includeSubDirectory + "/" + outputPathRaw;
                }
                if (!outputPathRaw.EndsWith(includeExtension))
                {
                    outputPathRaw += includeExtension;
                }

                var outputPath = GetNormalizedPath(outputPathRaw);
                if (!outputItems.Add(outputPath.ToString()))
                {
                    throw Error.InvalidOperation("Output path name conflict.");
                }
                using var stream = outputStreamProvider(outputPath.ToString());
                SaveDocument(recordDocument, stream);
            }

            // Create main document
            {
                var rootElement = CreateRootElement(_emitPatchBoilerplate ? GxmDocumentType.MetadataPatch : GxmDocumentType.Metadata);
                foreach (var outputItem in outputItems)
                {
                    var includeElement = new XElement(_naming.IncludeDirectiveElement);
                    includeElement.SetAttributeValue(_naming.IncludeDirectivePathAttribute, outputItem);
                    rootElement.Add(includeElement);
                }
                var mainDocument = new XDocument(rootElement);

                var outputPath = GetNormalizedPath(relativePath + "/" + mainFileName);

                using var stream = outputStreamProvider(outputPath.ToString());
                SaveDocument(mainDocument, stream);
            }

            static Path GetNormalizedPath(string path)
            {
                return Path.Implicit(path).Convert(IncludePathForm, check: true);
            }
        }

        private XElement CreateRootElement(GxmDocumentType documentType)
        {
            var rootElementName = documentType switch
            {
                GxmDocumentType.Metadata => _naming.MetadataRootElement,
                GxmDocumentType.MetadataPartial => _naming.MetadataPartialRootElement,
                GxmDocumentType.MetadataPatch => _naming.MetadataPatchRootElement,
                GxmDocumentType.MetadataPatchPartial => _naming.MetadataPatchPartialRootElement,
                _ => throw Error.Argument(nameof(documentType)),
            };

            var result = new XElement(rootElementName);
            if (documentType == GxmDocumentType.Metadata
                || documentType == GxmDocumentType.MetadataPatch)
            {
                result.SetAttributeValue(_naming.MetadataRootBasePathAttribute, _databaseType.BasePath);
            }
            return result;
        }

        #region Simple

        private XDocument CreateMetadataDocument()
        {
            var rootElement = CreateRootElement(_emitPatchBoilerplate ? GxmDocumentType.MetadataPatch : GxmDocumentType.Metadata);

            foreach (var x in GetSortedRecordTypes(_databaseType))
            {
                var e = CreateRecord(x);
                rootElement.Add(e);
            }

            return new XDocument(declaration: null, rootElement);
        }

        #endregion

        #region Multipart

        private XDocument CreateMetadataIncludeDocument(RecordType recordType)
        {
            var rootElement = CreateRootElement(_emitPatchBoilerplate ? GxmDocumentType.MetadataPatchPartial : GxmDocumentType.MetadataPartial);
            var e = CreateRecord(recordType);
            rootElement.Add(e);
            return new XDocument(rootElement);
        }

        #endregion

        private XElement CreateRecord(RecordType recordType)
        {
            var result = new XElement(_naming.RecordTypeElement);
            result.SetAttributeValue(_naming.RecordTypeNameAttribute, recordType.Name);
            if (EmitRecordPath)
            {
                result.SetAttributeValue(_naming.RecordTypePathAttribute, recordType.Path);
            }

            if (EmitInherits)
            {
                var inheritedRecordTypes = recordType.InheritedRecordTypes;
                if (inheritedRecordTypes.Any())
                {
                    var target = new XElement(_naming.InheritGroupElement);
                    result.Add(target);

                    foreach (var inheritedRecordType in inheritedRecordTypes)
                    {
                        var e = CreateInherit(inheritedRecordType);
                        target.Add(e);
                    }
                }
            }

            if (EmitExpressionVariables)
            {
                var declaredExpressionVariables = recordType.DeclaredExpressionVariables;
                if (declaredExpressionVariables.Any())
                {
                    var target = new XElement(_naming.ExpressionVariableGroupElement);
                    result.Add(target);

                    var expressionVariables = declaredExpressionVariables.OrderBy(x => x.Name, StringComparer.Ordinal);
                    foreach (var expressionVariable in expressionVariables)
                    {
                        var e = CreateExpressionVariable(expressionVariable);
                        target.Add(e);
                    }
                }
            }

            // create root group element and map
            var rootVarGroup = recordType.DeclaringDatabaseType.RootFieldVarGroup;
            var rootVarGroupElement = CreateGroup(rootVarGroup);
            var groupIdToElementMap = new Dictionary<int, XElement>();
            groupIdToElementMap.Add(rootVarGroup.Id, rootVarGroupElement);

            foreach (var field in recordType.DeclaredFieldTypes)
            {
                XElement varGroupElement;
                if (_emitVarGroups)
                {
                    varGroupElement = GetOrCreateGroupElementInTree(field.FieldVarGroup, groupIdToElementMap);
                }
                else
                {
                    varGroupElement = rootVarGroupElement;
                }
                var fieldElement = CreateField(field);
                varGroupElement.Add(fieldElement);
            }

            SortGroupElement(rootVarGroupElement);
            if (_emitRootVarGroup)
            {
                result.Add(rootVarGroupElement);
            }
            else
            {
                result.Add(rootVarGroupElement.Elements());
            }

            return result;
        }

        private void SortGroupElement(XElement element)
        {
            var sortedElements = element.Elements()
                .Select(x =>
                {
                    if (x.Name == _naming.VarGroupElement)
                    {
                        SortGroupElement(x);
                    }
                    return x;
                })
                .OrderBy(x => x.Name == _naming.VarGroupElement ? 0 : 1)
                .ThenBy(x => x.Attribute(_naming.VarGroupNameAttribute)?.Value == "Header"
                    && x.Attribute(_naming.VarGroupSystemAttribute)?.Value == "true" ? 0 : 1)
                .ThenBy(x => x.Attribute(_naming.FieldNameAttribute)?.Value, NaturalOrderStringComparer.Ordinal)
                .ToList();
            element.RemoveNodes();
            element.Add(sortedElements);
        }

        private XElement GetOrCreateGroupElementInTree(FieldVarGroup targetVarGroup,
            Dictionary<int, XElement> map)
        {
            if (map.TryGetValue(targetVarGroup.Id, out var result))
            {
                return result;
            }

            // build path
            var path = new Stack<FieldVarGroup>();
            {
                FieldVarGroup? n = targetVarGroup;
                while (n != null)
                {
                    path.Push(n);
                    n = n.Parent;
                }
            }

            XElement? pe = null;
            while (path.TryPop(out var g))
            {
                if (!map.TryGetValue(g.Id, out var ge))
                {
                    ge = CreateGroup(g);
                    map.Add(g.Id, ge);
                    pe!.Add(ge);
                }
                pe = ge;
            }

            Check.That(pe != null);
            return pe;
        }

        private XElement CreateInherit(RecordType recordType)
        {
            var result = new XElement(_naming.InheritElement);
            result.SetAttributeValue(_naming.InheritRecordTypeNameAttribute, recordType.Name);
            return result;
        }

        private XElement CreateExpressionVariable(ExpressionVariable expressionVariable)
        {
            var result = new XElement(_naming.ExpressionVariableElement);
            result.SetAttributeValue(_naming.ExpressionVariableNameAttribute, expressionVariable.Name);
            var descriptionElement = CreateDocumentationOrNull(expressionVariable.Documentation);
            if (descriptionElement != null)
            {
                result.Add(descriptionElement);
            }
            return result;
        }

        private XElement CreateField(FieldType fieldType)
        {
            var result = new XElement(_naming.FieldElement);
            result.SetAttributeValue(_naming.FieldNameAttribute, fieldType.Name);

            if (_emitVarPropertyAsAttr && EmitProperties)
            {
                result.SetAttributeValue(_naming.FieldVarClassAttribute, fieldType.VarClass);
                result.SetAttributeValue(_naming.FieldVarTypeAttribute, fieldType.VarType);
                if (!string.IsNullOrEmpty(fieldType.VarValue))
                {
                    result.SetAttributeValue(_naming.FieldVarValueAttribute, fieldType.VarValue);
                }
                if (!string.IsNullOrEmpty(fieldType.VarDefaultValue))
                {
                    result.SetAttributeValue(_naming.FieldVarDefaultValueAttribute, fieldType.VarDefaultValue);
                }
            }
            else if (EmitProperties)
            {
                result.Add(CreatePropertyElement(_naming.FieldVarClassElement, fieldType.VarClass));
                result.Add(CreatePropertyElement(_naming.FieldVarTypeElement, fieldType.VarType));
                result.Add(CreatePropertyElement(_naming.FieldVarValueElement, fieldType.VarValue));
                result.Add(CreatePropertyElement(_naming.FieldVarDefaultValueElement, fieldType.VarDefaultValue));
            }

            if (!_includeOnlyVarProperties && EmitProperties)
            {
                result.Add(CreatePropertyElement(_naming.FieldArzValueTypeElement,
                    SerializationUtilities.FormatToString(fieldType.ValueType)
                    ));
                result.Add(CreatePropertyElement(_naming.FieldArrayElement,
                    SerializationUtilities.FormatToString(fieldType.Array)));
            }

            var descriptionElement = CreateDocumentationOrNull(fieldType.Documentation);
            if (descriptionElement != null)
            {
                result.Add(descriptionElement);
            }

            return result;
        }

        private XElement? CreatePropertyElement(XName name, string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            var result = new XElement(name);
            result.SetAttributeValue(_naming.FieldPropertyValueAttribute, value);
            return result;
        }

        private XElement CreateGroup(FieldVarGroup varGroup)
        {
            var result = new XElement(_naming.VarGroupElement);
            if (!string.IsNullOrEmpty(varGroup.Name))
            {
                result.SetAttributeValue(_naming.VarGroupNameAttribute, varGroup.Name);
            }
            if (varGroup.System)
            {
                result.SetAttributeValue(_naming.VarGroupSystemAttribute, varGroup.System ? "true" : "false");
            }
            return result;
        }

        private XElement? CreateDocumentationOrNull(string? description)
        {
            if (!EmitDescription) return null;

            if (string.IsNullOrEmpty(description)) return null;

            var result = new XElement(_naming.DocumentationElement);
            result.Add(new XText(description));
            return result;
        }

        private static IEnumerable<RecordType> GetSortedRecordTypes(DatabaseType databaseType)
        {
            var recordTypes = databaseType
                .RecordTypes
                .OrderBy(x => x.Name, NaturalOrderStringComparer.Ordinal);
            return recordTypes;
        }

        private void SaveDocument(XDocument document, IO.Stream stream)
        {
            var settings = new System.Xml.XmlWriterSettings();
            settings.OmitXmlDeclaration = _omitXmlDeclaration;
            settings.Indent = true;
            settings.CloseOutput = true;
            settings.ConformanceLevel = System.Xml.ConformanceLevel.Document;
            settings.Encoding = System.Text.Encoding.UTF8;
            settings.NamespaceHandling = System.Xml.NamespaceHandling.OmitDuplicates;

            // using (var streamWriter = new IO.StreamWriter(stream))
            using (var writer = System.Xml.XmlWriter.Create(stream, settings))
            {
                document.WriteTo(writer);
            }
        }
    }
}
