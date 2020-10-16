using System;
using System.Globalization;
using System.Xml.Linq;

using Glacie.Data.Arz;
using Glacie.Diagnostics;
using Glacie.Metadata.Builder;

namespace Glacie.Metadata.Serialization
{
    using Naming = ElementNames;

    // TODO: might be actually a struct, as it is simple context.
    internal ref struct XmlMetadataReader
    {
        private readonly MetadataBuilder _metadataBuilder;
        private readonly string _path;
        private readonly XDocument _document;
        private readonly Func<string, GxmDocumentType, bool>? _processIncludeDirective;
        private readonly GxmDocumentType _type;

        public XmlMetadataReader(MetadataBuilder metadataBuilder,
            string path,
            XDocument document,
            Func<string, GxmDocumentType, bool>? processIncludeDirective = null,
            GxmDocumentType parentDocumentType = GxmDocumentType.Unknown)
        {
            _metadataBuilder = metadataBuilder;
            _path = path;
            _document = document;
            _processIncludeDirective = processIncludeDirective;
            _type = GetDocumentType(document, parentDocumentType);
        }

        private bool IsPatch => _type == GxmDocumentType.MetadataPatch
            || _type == GxmDocumentType.MetadataPatchPartial;

        private bool IsIncludeDirectiveAllowed
            => _processIncludeDirective != null
            && (_type == GxmDocumentType.Metadata || _type == GxmDocumentType.MetadataPatch);

        public void Read()
        {
            var rootElement = _document.Root;

            switch (_type)
            {
                case GxmDocumentType.Metadata:
                    if (!_metadataBuilder.IsEmpty)
                    {
                        throw Error.InvalidOperation("You can load metadata document only in empty metadata builder.");
                    }
                    break;

                case GxmDocumentType.MetadataPartial:
                case GxmDocumentType.MetadataPatch:
                case GxmDocumentType.MetadataPatchPartial:
                    break;

                default:
                    throw DiagnosticFactory
                    .ReaderError(GetLocation(rootElement), "Unknown document type.")
                    .AsException();
            }

            ReadMetadataRoot(rootElement);
        }

        private void ProcessInclude(string includePath, System.Xml.IXmlLineInfo xmlLineInfo)
        {
            if (_processIncludeDirective == null)
            {
                throw Error.InvalidOperation("Processing of include directives is not allowed.");
            }

            var success = _processIncludeDirective(includePath, _type);
            if (!success)
            {
                throw DiagnosticFactory.ReaderError(GetLocation(xmlLineInfo),
                    string.Format(CultureInfo.InvariantCulture,
                        "Unable to process include directive: \"{0}\" (resource not found?)", includePath))
                    .AsException();
            }
        }

        private void ReadMetadataRoot(XElement element)
        {
            string? basePath = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case "xmlns": break;

                    case Naming.MetadataRootBasePathAttribute:
                        ReadString(a, ref basePath);
                        if (basePath != null)
                        {
                            if (_metadataBuilder.BasePath.IsEmpty)
                            {
                                _metadataBuilder.BasePath = Path.Implicit(basePath);
                            }
                            else
                            {
                                var comparison = _metadataBuilder.IsCaseSensitivePath ? PathComparison.Ordinal : PathComparison.OrdinalIgnoreCase;
                                if (_metadataBuilder.BasePath.Equals(basePath, comparison))
                                {
                                    // no-op
                                }
                                else
                                {
                                    throw DiagnosticFactory
                                        .ReaderError(GetLocation(a), "MetadataBuilder has different base-path.")
                                        .AsException();
                                }
                            }
                        }
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
                    case Naming.IncludeDirectiveElement:
                        if (IsIncludeDirectiveAllowed)
                        {
                            var includePath = ReadIncludeDirective(e);
                            ProcessInclude(includePath, e);
                        }
                        else
                        {
                            throw DiagnosticFactory
                                .ReaderError(GetLocation(e), "Include directive is not allowed.")
                                .AsException();
                        }
                        break;

                    case Naming.RecordTypeElement:
                        ReadRecordType(e);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }
        }

        private string ReadIncludeDirective(XElement element)
        {
            DebugCheck.That(element.Name.LocalName == Naming.IncludeDirectiveElement);

            string? includePath = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.IncludeDirectivePathAttribute:
                        ReadStringNotEmpty(a, ref includePath);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            EnsureRequiredAttribute(ref includePath, element, Naming.IncludeDirectivePathAttribute);

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

            Check.That(includePath != null);
            return includePath;
        }

        private PathComparison GetPathComparison()
        {
            if (_metadataBuilder.IsCaseSensitivePath) return PathComparison.Ordinal;
            else return PathComparison.OrdinalIgnoreCase;
        }

        private void ReadRecordType(XElement element)
        {
            DebugCheck.That(element.Name.LocalName == Naming.RecordTypeElement);

            string? recordTypeName = null;
            string? recordTypePath = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.RecordTypeNameAttribute:
                        ReadStringNotEmpty(a, ref recordTypeName);
                        break;

                    case Naming.RecordTypePathAttribute:
                        ReadStringNotEmpty(a, ref recordTypePath);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            EnsureRequiredAttribute(ref recordTypeName, element, Naming.RecordTypeNameAttribute);
            Check.That(recordTypeName != null);

            // TODO: patch should allow add new record types, but currently is not supported.
            RecordTypeBuilder recordTypeBuilder;
            if (!IsPatch)
            {
                EnsureRequiredAttribute(ref recordTypePath, element, Naming.RecordTypePathAttribute);
                Check.That(recordTypePath != null);

                recordTypeBuilder = _metadataBuilder.DefineRecordType(recordTypeName);
                recordTypeBuilder.Path = Path.Implicit(recordTypePath);
            }
            else
            {
                recordTypeBuilder = _metadataBuilder.GetRecordType(recordTypeName);
                if (recordTypePath != null)
                {
                    if (!recordTypeBuilder.Path.Equals(recordTypePath, GetPathComparison()))
                    {
                        throw DiagnosticFactory
                            .ReaderError(GetLocation(element, _path), "Attempt to override record's type path.")
                            .AsException();
                    }
                }
            }

            bool hasInheritGroupElement = false;
            bool hasExpressionVariableGroupElement = false;
            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    case Naming.InheritGroupElement:
                        if (IsPatch)
                        {
                            throw DiagnosticFactory
                                .ReaderError(GetLocation(e, _path), "Changing inheritance is not supported in patch mode.")
                                .AsException();
                        }
                        if (hasInheritGroupElement)
                        {
                            throw DiagnosticFactory.ElementMustBeUsedOnlyOnce(GetLocation(e), e)
                                .AsException();
                        }
                        hasInheritGroupElement = true;
                        ReadInheritGroupElement(e, recordTypeBuilder);
                        break;

                    case Naming.ExpressionVariableGroupElement:
                        if (IsPatch)
                        {
                            throw DiagnosticFactory
                                .ReaderError(GetLocation(e, _path), "Changing expression variables is not supported in patch mode.")
                                .AsException();
                        }
                        if (hasExpressionVariableGroupElement)
                        {
                            throw DiagnosticFactory
                                .ElementMustBeUsedOnlyOnce(GetLocation(e), e)
                                .AsException();
                        }
                        hasExpressionVariableGroupElement = true;
                        ReadExpressionVariableGroup(e, recordTypeBuilder);
                        break;

                    case Naming.VarGroupElement:
                        ReadVarGroupElement(e, recordTypeBuilder, _metadataBuilder.RootFieldVarGroup);
                        break;

                    case Naming.FieldElement:
                        ReadField(e, recordTypeBuilder, _metadataBuilder.RootFieldVarGroup);
                        break;

                    case Naming.DocumentationElement:
                        recordTypeBuilder.Documentation = GetDocumentation(e);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }
        }

        private void ReadVarGroupElement(XElement element, RecordTypeBuilder recordTypeBuilder, FieldVarGroupBuilder group)
        {
            DebugCheck.That(element.Name.LocalName == Naming.VarGroupElement);

            string? varGroupName = null;
            bool? varGroupSystem = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.VarGroupNameAttribute:
                        ReadString(a, ref varGroupName);
                        break;

                    case Naming.VarGroupSystemAttribute:
                        ReadBoolean(a, ref varGroupSystem);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            FieldVarGroupBuilder targetGroup;
            if (!string.IsNullOrEmpty(varGroupName))
            {
                targetGroup = group.DefineFieldVarGroup(varGroupName, varGroupSystem ?? false);
            }
            else
            {
                if (varGroupSystem != null)
                {
                    throw DiagnosticFactory
                        .ReaderError(GetLocation(element), "var-group should not use system attribute without name.")
                        .AsException();
                }

                targetGroup = group;
            }

            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    case Naming.VarGroupElement:
                        ReadVarGroupElement(e, recordTypeBuilder, targetGroup);
                        break;

                    case Naming.FieldElement:
                        ReadField(e, recordTypeBuilder, targetGroup);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }
        }

        private void ReadInheritGroupElement(XElement element, RecordTypeBuilder recordType)
        {
            DebugCheck.That(element.Name.LocalName == Naming.InheritGroupElement);

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
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
                    case Naming.InheritElement:
                        ReadInheritElement(e, recordType);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }
        }

        private void ReadInheritElement(XElement element, RecordTypeBuilder recordType)
        {
            DebugCheck.That(element.Name.LocalName == Naming.InheritElement);

            string? inheritRecordTypeName = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.InheritRecordTypeNameAttribute:
                        ReadStringNotEmpty(a, ref inheritRecordTypeName);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            EnsureRequiredAttribute(ref inheritRecordTypeName, element, Naming.InheritRecordTypeNameAttribute);

            Check.That(inheritRecordTypeName != null);

            if (!_metadataBuilder.TryGetRecordType(inheritRecordTypeName, out var inheritRecordType))
            {
                inheritRecordType = _metadataBuilder.GetRecordTypeReference(inheritRecordTypeName);
            }
            recordType.AddInheritedFrom(inheritRecordType);

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
        }

        private void ReadExpressionVariableGroup(XElement element, RecordTypeBuilder recordType)
        {
            DebugCheck.That(element.Name.LocalName == Naming.ExpressionVariableGroupElement);

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
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
                    case Naming.ExpressionVariableElement:
                        ReadExpressionVariable(e, recordType);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }
        }

        private void ReadExpressionVariable(XElement element, RecordTypeBuilder recordType)
        {
            DebugCheck.That(element.Name.LocalName == Naming.ExpressionVariableElement);

            string? expressionVariableName = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.ExpressionVariableNameAttribute:
                        ReadStringNotEmpty(a, ref expressionVariableName);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            EnsureRequiredAttribute(ref expressionVariableName, element, Naming.ExpressionVariableNameAttribute);

            Check.That(expressionVariableName != null);

            var expressionVariable = recordType.DefineExpressionVariable(expressionVariableName);

            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    case Naming.DocumentationElement:
                        expressionVariable.Documentation = GetDocumentation(e);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }
        }

        private FieldTypeBuilder ReadField(XElement element, RecordTypeBuilder recordType, FieldVarGroupBuilder fieldVarGroup)
        {
            DebugCheck.That(element.Name.LocalName == Naming.FieldElement);

            string? fieldName = null;
            string? fieldVarClass = null;
            string? fieldVarType = null;
            string? fieldVarValue = null;
            string? fieldVarDefaultValue = null;
            ArzValueType? fieldArzValueType = null;
            bool? fieldArray = null;

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case Naming.FieldNameAttribute:
                        ReadStringNotEmpty(a, ref fieldName);
                        break;

                    case Naming.FieldVarClassAttribute:
                        ReadString(a, ref fieldVarClass);
                        break;

                    case Naming.FieldVarTypeAttribute:
                        ReadString(a, ref fieldVarType);
                        break;

                    case Naming.FieldVarValueAttribute:
                        ReadString(a, ref fieldVarValue);
                        break;

                    case Naming.FieldVarDefaultValueAttribute:
                        ReadString(a, ref fieldVarDefaultValue);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedAttribute(GetLocation(a), a)
                            .AsException();
                }
            }

            EnsureRequiredAttribute(ref fieldName, element, Naming.FieldNameAttribute);

            Check.That(fieldName != null);

            XElement? fieldVarClassElement = null;
            XElement? fieldVarTypeElement = null;
            XElement? fieldVarValueElement = null;
            XElement? fieldVarDefaultValueElement = null;
            XElement? fieldArzValueTypeElement = null;
            XElement? fieldArrayElement = null;
            XElement? documentationElement = null;

            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    case Naming.FieldVarClassElement:
                        ReadElementOnce(e, ref fieldVarClassElement);
                        break;

                    case Naming.FieldVarTypeElement:
                        ReadElementOnce(e, ref fieldVarTypeElement);
                        break;

                    case Naming.FieldVarValueElement:
                        ReadElementOnce(e, ref fieldVarValueElement);
                        break;

                    case Naming.FieldVarDefaultValueElement:
                        ReadElementOnce(e, ref fieldVarDefaultValueElement);
                        break;

                    case Naming.FieldArzValueTypeElement:
                        ReadElementOnce(e, ref fieldArzValueTypeElement);
                        break;

                    case Naming.FieldArrayElement:
                        ReadElementOnce(e, ref fieldArrayElement);
                        break;

                    case Naming.DocumentationElement:
                        ReadElementOnce(e, ref documentationElement);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedElement(GetLocation(e), e)
                            .AsException();
                }
            }

            if (fieldVarClassElement != null)
            {
                if (fieldVarClass != null) throw ConflictingElementError(fieldVarClassElement);
                ReadPropertyString(fieldVarClassElement, ref fieldVarClass);
            }

            if (fieldVarTypeElement != null)
            {
                if (fieldVarType != null) throw ConflictingElementError(fieldVarTypeElement);
                ReadPropertyString(fieldVarTypeElement, ref fieldVarType);
            }

            if (fieldVarValueElement != null)
            {
                if (fieldVarValue != null) throw ConflictingElementError(fieldVarValueElement);
                ReadPropertyString(fieldVarValueElement, ref fieldVarValue);
            }

            if (fieldVarDefaultValueElement != null)
            {
                if (fieldVarDefaultValue != null) throw ConflictingElementError(fieldVarDefaultValueElement);
                ReadPropertyString(fieldVarDefaultValueElement, ref fieldVarDefaultValue);
            }

            // TODO: handle all element with ReadElementOnce when possible

            if (fieldArzValueTypeElement != null)
            {
                ReadPropertyArzValueType(fieldArzValueTypeElement, ref fieldArzValueType);
            }

            if (fieldArrayElement != null)
            {
                ReadPropertyBoolean(fieldArrayElement, ref fieldArray);
            }

            FieldTypeBuilder? fieldInfo;
            if (IsPatch)
            {
                if (!recordType.TryGetField(fieldName, out fieldInfo))
                {
                    fieldInfo = recordType.DefineField(fieldName, fieldVarGroup);
                }
            }
            else
            {
                fieldInfo = recordType.DefineField(fieldName, fieldVarGroup);
            }

            if (fieldVarClass != null)
            {
                fieldInfo.VarClass = PatchOrThrow(fieldInfo.VarClass, EmptyToNull(fieldVarClass),
                    fieldVarClassElement ?? element, Naming.FieldVarClassElement);
            }

            if (fieldVarType != null)
            {
                fieldInfo.VarType = PatchOrThrow(fieldInfo.VarType, EmptyToNull(fieldVarType),
                    fieldVarTypeElement ?? element, Naming.FieldVarTypeElement);
            }

            if (fieldVarValue != null)
            {
                fieldInfo.VarValue = PatchOrThrow(fieldInfo.VarValue, EmptyToNull(fieldVarValue),
                    fieldVarValueElement ?? element, Naming.FieldVarValueElement);
            }

            if (fieldVarDefaultValue != null)
            {
                fieldInfo.VarDefaultValue = PatchOrThrow(fieldInfo.VarDefaultValue, EmptyToNull(fieldVarDefaultValue),
                    fieldVarDefaultValueElement ?? element, Naming.FieldVarDefaultValueElement);
            }

            if (fieldArzValueType != null)
            {
                fieldInfo.ValueType = PatchOrThrow(fieldInfo.ValueType, fieldArzValueType,
                    fieldArzValueTypeElement ?? element, Naming.FieldArzValueTypeElement);
            }

            if (fieldArray != null)
            {
                fieldInfo.Array = PatchOrThrow(fieldInfo.Array, fieldArray,
                    fieldArrayElement ?? element, Naming.FieldArrayElement);
            }

            if (documentationElement != null)
            {
                // TODO: patch or throw
                fieldInfo.Documentation = GetDocumentation(documentationElement);
            }

            return fieldInfo;
        }

        private string? PatchOrThrow(string? currentValue, string? newValue, XElement location, string propertyName)
        {
            if (IsPatch)
            {
                if (currentValue != null && currentValue != newValue)
                {
                    throw DiagnosticFactory
                        .PatchingConflict(GetLocation(location), propertyName, currentValue, newValue)
                        .AsException();
                }
            }
            return newValue;
        }

        private ArzValueType? PatchOrThrow(ArzValueType? currentValue, ArzValueType? newValue, XElement location, string propertyName)
        {
            if (IsPatch)
            {
                if (currentValue != null && currentValue != newValue)
                {
                    throw DiagnosticFactory
                        .PatchingConflict(GetLocation(location), propertyName, currentValue?.ToString(), newValue?.ToString())
                        .AsException();
                }
            }
            return newValue;
        }

        private bool? PatchOrThrow(bool? currentValue, bool? newValue, XElement location, string propertyName)
        {
            if (IsPatch)
            {
                if (currentValue != null && currentValue != newValue)
                {
                    throw DiagnosticFactory
                        .PatchingConflict(GetLocation(location), propertyName, currentValue?.ToString(), newValue?.ToString())
                        .AsException();
                }
            }
            return newValue;
        }

        private string? GetDocumentation(XElement element)
        {
            DebugCheck.That(element.Name.LocalName == Naming.DocumentationElement);

            foreach (var a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
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

            return DocumentationUtilities.Normalize(element.Value);
        }

        private void ReadString(XAttribute attribute, ref string? value)
        {
            var attributeValue = attribute.Value;
            Check.That(attributeValue != null);
            Check.That(value == null);
            value = attributeValue.Trim();
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

        private void ReadBoolean(XAttribute attribute, ref bool? value)
        {
            var attributeValue = attribute.Value;
            Check.That(attributeValue != null);
            Check.That(value == null);
            attributeValue = attributeValue.Trim();
            if (attributeValue == "true")
            {
                value = true;
            }
            else if (attributeValue == "false")
            {
                value = false;
            }
            else throw DiagnosticFactory
                    .InvalidAttributeValue(GetLocation(attribute), "Invalid attribute value (boolean - only \"true\" or \"false\") accepted.")
                    .AsException();
        }

        private void ReadPropertyString(XElement element, ref string? value)
        {
            var valueAttribute = element.Attribute(Naming.FieldPropertyValueAttribute);
            if (valueAttribute == null)
            {
                throw DiagnosticFactory
                    .ElementMustHaveAttribute(GetLocation(element), element, Naming.FieldPropertyValueAttribute)
                    .AsException();
            }
            ReadString(valueAttribute, ref value);
        }

        private void ReadPropertyArzValueType(XElement element, ref ArzValueType? value)
        {
            string? v = null;
            ReadPropertyString(element, ref v);
            if (value == null)
            {
                if (string.IsNullOrEmpty(v)) value = null;
                else if (v == "int" || v == "integer") value = ArzValueType.Integer;
                else if (v == "real") value = ArzValueType.Real;
                else if (v == "string") value = ArzValueType.String;
                else if (v == "bool" || v == "boolean") value = ArzValueType.Boolean;
                else throw DiagnosticFactory
                        .InvalidAttributeValue(GetLocation(element), message: "Valid values is: \"int\", \"real\", \"string\" or \"bool\".")
                        .AsException();
            }
            else throw Error.Unreachable();
        }

        private void ReadPropertyBoolean(XElement element, ref bool? value)
        {
            string? v = null;
            ReadPropertyString(element, ref v);
            if (value == null)
            {
                if (string.IsNullOrEmpty(v)) value = null;
                else if (v == "false") value = false;
                else if (v == "true") value = true;
                else throw DiagnosticFactory
                        .InvalidAttributeValue(GetLocation(element), message: "Valid values is: \"int\", \"real\", \"string\" or \"bool\".")
                        .AsException();
            }
            else throw Error.Unreachable();
        }

        private void ReadElementOnce(XElement element, ref XElement? value)
        {
            Check.That(element != null);
            if (value == null) value = element;
            else
            {
                throw DiagnosticFactory
                    .ElementMustBeUsedOnlyOnce(GetLocation(element), element)
                    .AsException();
            }
        }

        private Exception ConflictingElementError(XElement element)
        {
            return DiagnosticFactory
                .YouMustSpecifyElementOrAttributeButNotBoth(GetLocation(element), element)
                .AsException();
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

        private static string? EmptyToNull(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value;
        }

        private Location GetLocation(System.Xml.IXmlLineInfo xmlLineInfo)
            => GetLocation(xmlLineInfo, _path);

        private static Location GetLocation(System.Xml.IXmlLineInfo xmlLineInfo, string? path)
        {
            if (xmlLineInfo.HasLineInfo())
            {
                return Location.File(path, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
            }
            else if (path != null)
            {
                return Location.JustFile(path);
            }
            else return Location.None;
        }

        private static GxmDocumentType GetDocumentType(XDocument document, GxmDocumentType parentDocumentType)
        {
            var rootElementName = document.Root.Name.LocalName;
            var rootNamespace = document.Root.Name.NamespaceName;

            if (rootNamespace == Naming.MetadataNamespace)
            {
                if (rootElementName == Naming.MetadataRootElement)
                {
                    return GxmDocumentType.Metadata;
                }
                else if (rootElementName == Naming.MetadataPartialRootElement)
                {
                    return GxmDocumentType.MetadataPartial;
                }
                else return GxmDocumentType.Unknown;
            }
            else if (rootNamespace == Naming.MetadataPatchNamespace)
            {
                if (rootElementName == Naming.MetadataPatchRootElement)
                {
                    return GxmDocumentType.MetadataPatch;
                }
                else if (rootElementName == Naming.MetadataPartialRootElement)
                {
                    return GxmDocumentType.MetadataPatchPartial;
                }
                else return GxmDocumentType.Unknown;
            }
            else if (rootElementName == Naming.MetadataRootElement)
            {
                return GxmDocumentType.Metadata;
            }
            else if (rootElementName == Naming.MetadataPatchRootElement)
            {
                return GxmDocumentType.MetadataPatch;
            }
            else if (rootElementName == Naming.MetadataPartialRootElement)
            {
                if (parentDocumentType == GxmDocumentType.MetadataPatch)
                {
                    return GxmDocumentType.MetadataPatchPartial;
                }
                else if (parentDocumentType == GxmDocumentType.Metadata)
                {
                    return GxmDocumentType.MetadataPartial;
                }
                else return GxmDocumentType.Unknown;
            }
            return GxmDocumentType.Unknown;
        }
    }
}
