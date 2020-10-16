using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Glacie.Data.Metadata.V1;

namespace Glacie.Lab.Metadata
{
    internal sealed class XmlDatabaseDefinitionWriter
    {
        private readonly ElementNames _names = new ElementNames();


        public XDocument Write(DatabaseType databaseDefinition)
        {
            var document = new XDocument();

            var rootElement = CreateDatabase(databaseDefinition);
            document.Add(rootElement);

            return document;
        }

        private XElement CreateDatabase(DatabaseType databaseDefinition)
        {
            var result = new XElement(_names.DatabaseTypeElementName);

            var recordDefinitions = databaseDefinition
                .RecordTypes
                .OrderBy(x => x.Name, NaturalOrderStringComparer.Ordinal);

            foreach (var x in recordDefinitions)
            {
                var e = CreateRecord(x);
                result.Add(e);
            }

            return result;
        }

        private XElement CreateRecord(RecordType recordDefinition)
        {
            var result = new XElement(_names.RecordTypeElementName);
            result.SetAttributeValue(_names.NameAttributeName, recordDefinition.Name);

            var inheritedRecordDefinitions = recordDefinition.InheritedRecordDefinitions;
            if (inheritedRecordDefinitions.Any())
            {
                var target = new XElement(_names.InheritGroupElementName);
                result.Add(target);

                foreach (var inheritedRecordDefinition in inheritedRecordDefinitions)
                {
                    var e = CreateInherit(inheritedRecordDefinition);
                    target.Add(e);
                }
            }

            var declaredExpressionVariables = recordDefinition.DeclaredExpressionVariables;
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
            var rootGroupDefinition = recordDefinition.DeclaringDatabaseType.RootFieldGroupDefinition;
            var rootGroupElement = CreateGroup(rootGroupDefinition);
            var groupIdToElementMap = new Dictionary<int, XElement>();
            groupIdToElementMap.Add(rootGroupDefinition.Id, rootGroupElement);

            var orderedFieldDefinitions = recordDefinition
                .DeclaredFieldDefinitions;
            // .OrderBy(x => x.Name, NaturalOrderStringComparer.Ordinal);
            foreach (var field in orderedFieldDefinitions)
            {
                var groupElement = GetOrCreateGroupElementInTree(field.FieldGroupDefinition, rootGroupDefinition, groupIdToElementMap);
                var e = CreateField(field);
                groupElement.Add(e);
            }

            SortGroupElement(rootGroupElement);
            result.Add(rootGroupElement);

            return result;
        }

        private void SortGroupElement(XElement element)
        {
            var sortedElements = element.Elements()
                .Select(x =>
                {
                    if (x.Name == _names.GroupElementName)
                    {
                        SortGroupElement(x);
                    }
                    return x;
                })
                .OrderBy(x => x.Name == _names.GroupElementName ? 0 : 1)
                .ThenBy(x => x.Attribute(_names.NameAttributeName)?.Value == "Header"
                    && x.Attribute(_names.SystemAttributeName)?.Value == "true" ? 0 : 1)
                .ThenBy(x => x.Attribute(_names.NameAttributeName)?.Value, NaturalOrderStringComparer.Ordinal)
                .ToList();
            element.RemoveNodes();
            element.Add(sortedElements);
        }

        private XElement GetOrCreateGroupElementInTree(FieldGroupDefinition targetGroupDefinition,
            FieldGroupDefinition rootGroupDefinition, Dictionary<int, XElement> map)
        {
            if (map.TryGetValue(targetGroupDefinition.Id, out var result))
            {
                return result;
            }

            // build path
            var path = new Stack<FieldGroupDefinition>();
            {
                FieldGroupDefinition? n = targetGroupDefinition;
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

        private XElement CreateInherit(RecordType recordDefinition)
        {
            var result = new XElement(_names.InheritElementName);
            result.SetAttributeValue(_names.RecordAttributeName, recordDefinition.Name);
            return result;
        }

        private XElement CreateExpressionVariable(ExpressionVariableDeclaration expressionVariableDefinition)
        {
            var result = new XElement(_names.ExpressionVariableElementName);
            result.SetAttributeValue(_names.NameAttributeName, expressionVariableDefinition.Name);
            var descriptionElement = CreateDescriptionOrNull(expressionVariableDefinition.Description);
            if (descriptionElement != null)
            {
                result.Add(descriptionElement);
            }
            return result;
        }

        private XElement CreateField(FieldType fieldDefinition)
        {
            var result = new XElement(_names.FieldTypeElementName);
            result.SetAttributeValue(_names.NameAttributeName, fieldDefinition.Name);

            result.SetAttributeValue(_names.ValueTypeAttributeName, fieldDefinition.ValueType);
            result.SetAttributeValue(_names.ArrayAttributeName, fieldDefinition.Array);

            if (!string.IsNullOrEmpty(fieldDefinition.VarClass))
            {
                result.SetAttributeValue(_names.VarClassAttributeName, fieldDefinition.VarClass);
            }

            if (!string.IsNullOrEmpty(fieldDefinition.VarType))
            {
                result.SetAttributeValue(_names.VarTypeAttributeName, fieldDefinition.VarType);
            }

            if (!string.IsNullOrEmpty(fieldDefinition.VarValue))
            {
                result.SetAttributeValue(_names.VarValueAttributeName, fieldDefinition.VarValue);
            }

            if (!string.IsNullOrEmpty(fieldDefinition.VarDefaultValue))
            {
                result.SetAttributeValue(_names.VarDefaultValueAttributeName, fieldDefinition.VarDefaultValue);
            }

            var descriptionElement = CreateDescriptionOrNull(fieldDefinition.Description);
            if (descriptionElement != null)
            {
                result.Add(descriptionElement);
            }

            return result;
        }

        private XElement CreateGroup(FieldGroupDefinition fieldGroupDefinition)
        {
            var result = new XElement(_names.GroupElementName);
            if (!string.IsNullOrEmpty(fieldGroupDefinition.Name))
            {
                result.SetAttributeValue(_names.NameAttributeName, fieldGroupDefinition.Name);
            }
            if (fieldGroupDefinition.System)
            {
                result.SetAttributeValue(_names.SystemAttributeName, fieldGroupDefinition.System ? "true" : "false");
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

        private sealed class ElementNames
        {
            public XName DatabaseTypeElementName { get; }
            public XName RecordTypeElementName { get; }
            public XName InheritGroupElementName { get; }
            public XName InheritElementName { get; }
            public XName FieldTypeElementName { get; }
            public XName DescriptionElementName { get; }
            public XName ExpressionVariableGroupElementName { get; }
            public XName ExpressionVariableElementName { get; }
            public XName GroupElementName { get; }

            public XName NameAttributeName { get; }
            public XName RecordAttributeName { get; }
            public XName SystemAttributeName { get; }

            public XName ValueTypeAttributeName { get; }
            public XName ArrayAttributeName { get; }

            public XName VarClassAttributeName { get; }
            public XName VarTypeAttributeName { get; }
            public XName VarValueAttributeName { get; }
            public XName VarDefaultValueAttributeName { get; }

            public ElementNames()
                : this("urn:glacie:metadata:v1")
            { }

            public ElementNames(XNamespace @namespace)
            {
                DatabaseTypeElementName = @namespace + "database-type";
                RecordTypeElementName = @namespace + "record-type";
                FieldTypeElementName = @namespace + "field-type";

                InheritGroupElementName = @namespace + "inherits";
                InheritElementName = @namespace + "inherit";
                DescriptionElementName = @namespace + "description";
                ExpressionVariableGroupElementName = @namespace + "expression-variables";
                ExpressionVariableElementName = @namespace + "expression-variable";
                GroupElementName = @namespace + "group";

                NameAttributeName = "name";
                RecordAttributeName = "record";
                SystemAttributeName = "system";

                ValueTypeAttributeName = "value-type";
                ArrayAttributeName = "array";

                VarClassAttributeName = "var-class";
                VarTypeAttributeName = "var-type";
                VarValueAttributeName = "var-value";
                VarDefaultValueAttributeName = "var-default-value";
            }

        }
    }
}
