using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Glacie.Metadata.Serialization
{
    // TODO: (XmlMetadataWriter) should be used as:
    // new XmlMetadataWriter(databaseType, options - include diagnostics).Write(to_stream) ? would it better?
    [Obsolete("Refactor XmlMetadataWriter. See comments above.")]
    public sealed class __XmlMetadataWriter
    {
        internal const Path1Form IncludePathForm = Path1Form.Relative | Path1Form.Strict | Path1Form.Normalized | Path1Form.DirectorySeparator;

        private readonly __ElementNames _names;
        private readonly bool _includeOnlyVarProperties;
        private readonly bool _varPropertiesAsAttributes;
        private readonly bool _varGroups;
        private readonly bool _rootVarGroup;

        public __XmlMetadataWriter(
            bool includeOnlyVarProperties,
            bool varPropertiesAsAttributes,
            bool varGroups,
            bool rootVarGroup)
        {
            _names = new __ElementNames(false);
            _includeOnlyVarProperties = includeOnlyVarProperties;
            _varPropertiesAsAttributes = varPropertiesAsAttributes;
            _varGroups = varGroups;
            _rootVarGroup = rootVarGroup;
        }

        public XDocument Write(DatabaseType databaseType)
        {
            var document = new XDocument();

            var rootElement = CreateDatabase(databaseType);
            document.Add(rootElement);

            return document;
        }

        public IReadOnlyCollection<(string Path, XDocument Document)> WriteMultipart(
            DatabaseType databaseType,
            string? mainFileName = null, string? includeSubDirectory = null)
        {
            var relativePath = ".";
            if (string.IsNullOrEmpty(mainFileName)) mainFileName = "!metadata";
            if (System.IO.Path.GetExtension(mainFileName) != ".gxmd")
            {
                mainFileName += ".gxmd";
            }

            var output = new List<(string Path, XDocument Document)>();
            foreach (var recordType in GetSortedRecordTypes(databaseType))
            {
                var recordDocument = CreateIncludeDocument(recordType);

                // TODO: Adjust this.
                string includePathRaw = recordType.Name
                        // .Replace('.', '/')
                        .Replace(' ', '_');
                if (string.IsNullOrEmpty(includeSubDirectory))
                {
                    includePathRaw = relativePath + "/" + includePathRaw;
                }
                else
                {
                    includePathRaw = relativePath + "/" + includeSubDirectory + "/" + includePathRaw;
                }
                if (!includePathRaw.EndsWith(".gxmdi"))
                {
                    includePathRaw += ".gxmdi";
                }

                var includePath = GetNormalizedPath(includePathRaw);
                (string Path, XDocument Document) resultItem = (includePath.ToString()!, recordDocument);
                output.Add(resultItem);
            }

            // Create main document
            {
                var document = new XDocument();
                var rootElement = new XElement(_names.MetadataRootElementName);
                foreach (var outputItem in output)
                {
                    var includeElement = new XElement(_names.IncludeElementName);
                    includeElement.SetAttributeValue(_names.IncludePathAttributeName, outputItem.Path);
                    rootElement.Add(includeElement);
                }
                document.Add(rootElement);

                var includePath = GetNormalizedPath(relativePath + "/" + mainFileName);

                (string Path, XDocument Document) resultItem = (includePath.ToString()!, document);
                output.Add(resultItem);
            }

            return output;

            static Path1 GetNormalizedPath(string path)
            {
                var includePath = Path1.From(path).ToForm(IncludePathForm);
                Check.That(includePath.IsInForm(IncludePathForm));
                return includePath;
            }
        }

        #region Multi Document Output

        private XDocument CreateIncludeDocument(RecordType recordType)
        {
            var result = new XDocument();

            var rootElement = new XElement(_names.MetadataIncludeRootElementName);
            var e = CreateRecord(recordType);
            rootElement.Add(e);

            result.Add(rootElement);
            return result;
        }

        #endregion

        private XElement CreateDatabase(DatabaseType databaseType)
        {
            var result = new XElement(_names.MetadataRootElementName);

            foreach (var x in GetSortedRecordTypes(databaseType))
            {
                var e = CreateRecord(x);
                result.Add(e);
            }

            return result;
        }

        private XElement CreateRecord(RecordType recordType)
        {
            var result = new XElement(_names.RecordTypeElementName);
            result.SetAttributeValue(_names.NameAttributeName, recordType.Name);

            var inheritedRecordTypes = recordType.InheritedRecordTypes;
            if (inheritedRecordTypes.Any())
            {
                var target = new XElement(_names.InheritGroupElementName);
                result.Add(target);

                foreach (var inheritedRecordType in inheritedRecordTypes)
                {
                    var e = CreateInherit(inheritedRecordType);
                    target.Add(e);
                }
            }

            var declaredExpressionVariables = recordType.DeclaredExpressionVariables;
            if (declaredExpressionVariables.Any())
            {
                var target = new XElement(_names.ExpressionVariableGroupElementName);
                result.Add(target);

                var expressionVariables = declaredExpressionVariables.OrderBy(x => x.Name, StringComparer.Ordinal);
                foreach (var expressionVariable in expressionVariables)
                {
                    var e = CreateExpressionVariable(expressionVariable);
                    target.Add(e);
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
                if (_varGroups)
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
            if (_rootVarGroup)
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
                    if (x.Name == _names.VarGroupElementName)
                    {
                        SortGroupElement(x);
                    }
                    return x;
                })
                .OrderBy(x => x.Name == _names.VarGroupElementName ? 0 : 1)
                .ThenBy(x => x.Attribute(_names.NameAttributeName)?.Value == "Header"
                    && x.Attribute(_names.SystemAttributeName)?.Value == "true" ? 0 : 1)
                .ThenBy(x => x.Attribute(_names.NameAttributeName)?.Value, NaturalOrderStringComparer.Ordinal)
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
            var result = new XElement(_names.InheritElementName);
            result.SetAttributeValue(_names.RecordAttributeName, recordType.Name);
            return result;
        }

        private XElement CreateExpressionVariable(ExpressionVariable expressionVariable)
        {
            var result = new XElement(_names.ExpressionVariableElementName);
            result.SetAttributeValue(_names.NameAttributeName, expressionVariable.Name);
            var descriptionElement = CreateDescriptionOrNull(expressionVariable.Documentation);
            if (descriptionElement != null)
            {
                result.Add(descriptionElement);
            }
            return result;
        }

        private XElement CreateField(FieldType fieldType)
        {
            var result = new XElement(_names.FieldTypeElementName);
            result.SetAttributeValue(_names.NameAttributeName, fieldType.Name);

            if (_varPropertiesAsAttributes)
            {
                result.SetAttributeValue(_names.VarClassAttributeName, fieldType.VarClass);
                result.SetAttributeValue(_names.VarTypeAttributeName, fieldType.VarType);
                if (!string.IsNullOrEmpty(fieldType.VarValue))
                {
                    result.SetAttributeValue(_names.VarValueAttributeName, fieldType.VarValue);
                }
                if (!string.IsNullOrEmpty(fieldType.VarDefaultValue))
                {
                    result.SetAttributeValue(_names.VarDefaultValueAttributeName, fieldType.VarDefaultValue);
                }
            }
            else
            {
                result.Add(CreatePropertyElement(_names.VarClassElementName, fieldType.VarClass));
                result.Add(CreatePropertyElement(_names.VarTypeElementName, fieldType.VarType));
                result.Add(CreatePropertyElement(_names.VarValueElementName, fieldType.VarValue));
                result.Add(CreatePropertyElement(_names.VarDefaultValueElementName, fieldType.VarDefaultValue));
            }

            if (!_includeOnlyVarProperties)
            {
                result.Add(CreatePropertyElement(_names.ArzValueTypeElementName,
                    SerializationUtilities.FormatToString(fieldType.ValueType)
                    ));
                result.Add(CreatePropertyElement(_names.ArrayElementName,
                    SerializationUtilities.FormatToString(fieldType.Array)));
            }

            var descriptionElement = CreateDescriptionOrNull(fieldType.Documentation);
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
            result.SetAttributeValue(_names.PropertyValueAttributeName, value);
            return result;
        }

        private XElement CreateGroup(FieldVarGroup FieldVarGroup)
        {
            var result = new XElement(_names.VarGroupElementName);
            if (!string.IsNullOrEmpty(FieldVarGroup.Name))
            {
                result.SetAttributeValue(_names.NameAttributeName, FieldVarGroup.Name);
            }
            if (FieldVarGroup.System)
            {
                result.SetAttributeValue(_names.SystemAttributeName, FieldVarGroup.System ? "true" : "false");
            }
            return result;
        }


        private XElement? CreateDescriptionOrNull(string? description)
        {
            if (string.IsNullOrEmpty(description)) return null;

            var result = new XElement(_names.DescriptionElementName);
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
    }
}
