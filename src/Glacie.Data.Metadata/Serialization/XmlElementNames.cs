using System.Xml.Linq;

namespace Glacie.Metadata.Serialization
{
    internal readonly struct XmlElementNames
    {
        public static XNamespace MetadataNamespace => "urn:glacie:metadata:v1";
        public static XNamespace MetadataPatchNamespace => "urn:glacie:metadata:patch:v1";


        public readonly XName MetadataRootElement;
        public readonly XName MetadataIncludeRootElement;

        public readonly XName IncludeElement;
        public readonly XName IncludePathAttribute;


        public readonly XName RecordTypeElement;
        public readonly XName InheritGroupElement;
        public readonly XName InheritElement;
        public readonly XName FieldTypeElement;
        public readonly XName DescriptionElement;
        public readonly XName ExpressionVariableGroupElement;
        public readonly XName ExpressionVariableElement;
        public readonly XName VarGroupElement;

        public readonly XName NameAttribute;
        public readonly XName RecordAttribute;
        public readonly XName SystemAttribute;

        public readonly XName PropertyValueAttribute;

        // var-* properties (template properties)
        public readonly XName VarClassAttribute;
        public readonly XName VarClassElement;
        public readonly XName VarTypeAttribute;
        public readonly XName VarTypeElement;
        public readonly XName VarValueAttribute;
        public readonly XName VarValueElement;
        public readonly XName VarDefaultValueAttribute;
        public readonly XName VarDefaultValueElement;

        public readonly XName ArzValueTypeElement;
        public readonly XName ArrayElement;


        public XmlElementNames(bool patch)
            : this(!patch ? "urn:glacie:metadata:v1" : "urn:glacie:metadata:patch:v1")
        { }

        public XmlElementNames(XNamespace @namespace)
        {
            MetadataRootElement = @namespace + "metadata";
            MetadataIncludeRootElement = @namespace + "include";

            IncludeElement = @namespace + "include";
            IncludePathAttribute = "path";

            RecordTypeElement = @namespace + "record-type";
            FieldTypeElement = @namespace + "field";
            DescriptionElement = @namespace + "description";

            InheritGroupElement = @namespace + "inherits";
            InheritElement = @namespace + "inherit";
            ExpressionVariableGroupElement = @namespace + "expression-variables";
            ExpressionVariableElement = @namespace + "expression-variable";
            VarGroupElement = @namespace + "var-group";

            ArzValueTypeElement = @namespace + "arz-value-type";
            ArrayElement = @namespace + "array";

            VarClassAttribute = "var-class";
            VarClassElement = @namespace + VarClassAttribute.LocalName;
            VarTypeAttribute = "var-type";
            VarTypeElement = @namespace + VarTypeAttribute.LocalName;
            VarValueAttribute = "var-value";
            VarValueElement = @namespace + VarValueAttribute.LocalName;
            VarDefaultValueAttribute = "var-default-value";
            VarDefaultValueElement = @namespace + VarDefaultValueAttribute.LocalName;

            NameAttribute = "name";
            RecordAttribute = "record";
            SystemAttribute = "system";

            PropertyValueAttribute = "value";
        }
    }
}
