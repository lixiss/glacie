using System.Xml.Linq;

namespace Glacie.Metadata.Serialization
{
    internal sealed class __ElementNames
    {
        public XName MetadataRootElementName { get; }
        public XName MetadataIncludeRootElementName { get; }

        public XName IncludeElementName { get; }
        public XName IncludePathAttributeName { get; }


        public XName RecordTypeElementName { get; }
        public XName InheritGroupElementName { get; }
        public XName InheritElementName { get; }
        public XName FieldTypeElementName { get; }
        public XName DescriptionElementName { get; }
        public XName ExpressionVariableGroupElementName { get; }
        public XName ExpressionVariableElementName { get; }
        public XName VarGroupElementName { get; }

        public XName NameAttributeName { get; }
        public XName RecordAttributeName { get; }
        public XName SystemAttributeName { get; }

        public XName PropertyValueAttributeName { get; }

        // var-* properties (template properties)
        public XName VarClassAttributeName { get; }
        public XName VarClassElementName { get; }
        public XName VarTypeAttributeName { get; }
        public XName VarTypeElementName { get; }
        public XName VarValueAttributeName { get; }
        public XName VarValueElementName { get; }
        public XName VarDefaultValueAttributeName { get; }
        public XName VarDefaultValueElementName { get; }

        public XName ArzValueTypeElementName { get; }
        public XName ArrayElementName { get; }

        public __ElementNames(bool patch)
            : this(!patch ? "urn:glacie:metadata:v1" : "urn:glacie:metadata:patch:v1")
        { }

        private __ElementNames(XNamespace @namespace)
        {
            MetadataRootElementName = @namespace + "metadata";
            MetadataIncludeRootElementName = @namespace + "include";

            IncludeElementName = @namespace + "include";
            IncludePathAttributeName = "path";

            RecordTypeElementName = @namespace + "record-type";
            FieldTypeElementName = @namespace + "field";
            DescriptionElementName = @namespace + "description";

            InheritGroupElementName = @namespace + "inherits";
            InheritElementName = @namespace + "inherit";
            ExpressionVariableGroupElementName = @namespace + "expression-variables";
            ExpressionVariableElementName = @namespace + "expression-variable";
            VarGroupElementName = @namespace + "var-group";

            ArzValueTypeElementName = @namespace + "arz-value-type";
            ArrayElementName = @namespace + "array";

            VarClassAttributeName = "var-class";
            VarClassElementName = @namespace + VarClassAttributeName.LocalName;
            VarTypeAttributeName = "var-type";
            VarTypeElementName = @namespace + VarTypeAttributeName.LocalName;
            VarValueAttributeName = "var-value";
            VarValueElementName = @namespace + VarValueAttributeName.LocalName;
            VarDefaultValueAttributeName = "var-default-value";
            VarDefaultValueElementName = @namespace + VarDefaultValueAttributeName.LocalName;

            NameAttributeName = "name";
            RecordAttributeName = "record";
            SystemAttributeName = "system";

            PropertyValueAttributeName = "value";
        }
    }
}
