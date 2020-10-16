using System.Xml.Linq;

namespace Glacie.Metadata.Serialization
{
    internal sealed class XElementNames
    {
        public static XNamespace MetadataNamespace => ElementNames.MetadataNamespace;
        public static XNamespace MetadataPatchNamespace => ElementNames.MetadataPatchNamespace;

        public XName MetadataRootElement { get; }
        public XName MetadataPatchRootElement { get; }
        public XName MetadataPartialRootElement { get; }
        public XName MetadataPatchPartialRootElement { get; }
        public XName MetadataRootBasePathAttribute { get; }

        public XName IncludeDirectiveElement { get; }
        public XName IncludeDirectivePathAttribute { get; }
        public XName RecordTypeElement { get; }
        public XName RecordTypeNameAttribute { get; }
        public XName RecordTypePathAttribute { get; }
        public XName VarGroupElement { get; }
        public XName VarGroupNameAttribute { get; }
        public XName VarGroupSystemAttribute { get; }
        public XName InheritGroupElement { get; }
        public XName InheritElement { get; }
        public XName InheritRecordTypeNameAttribute { get; }
        public XName ExpressionVariableGroupElement { get; }
        public XName ExpressionVariableElement { get; }
        public XName ExpressionVariableNameAttribute { get; }
        public XName DocumentationElement { get; }
        public XName FieldElement { get; }
        public XName FieldNameAttribute { get; }
        public XName FieldVarClassAttribute { get; }
        public XName FieldVarTypeAttribute { get; }
        public XName FieldVarValueAttribute { get; }
        public XName FieldVarDefaultValueAttribute { get; }

        // Field Property
        public XName FieldPropertyValueAttribute { get; }

        // Field Properties
        public XName FieldVarClassElement { get; }
        public XName FieldVarTypeElement { get; }
        public XName FieldVarValueElement { get; }
        public XName FieldVarDefaultValueElement { get; }
        public XName FieldArzValueTypeElement { get; }
        public XName FieldArrayElement { get; }

        public XElementNames(XNamespace @namespace)
        {
            MetadataRootElement = @namespace + ElementNames.MetadataRootElement;
            MetadataPartialRootElement = @namespace + ElementNames.MetadataPartialRootElement;
            MetadataPatchRootElement = @namespace + ElementNames.MetadataPatchRootElement;
            MetadataPatchPartialRootElement = @namespace + ElementNames.MetadataPatchPartialRootElement;
            MetadataRootBasePathAttribute = ElementNames.MetadataRootBasePathAttribute;
            IncludeDirectiveElement = @namespace + ElementNames.IncludeDirectiveElement;
            IncludeDirectivePathAttribute = ElementNames.IncludeDirectivePathAttribute;
            RecordTypeElement = @namespace + ElementNames.RecordTypeElement;
            RecordTypeNameAttribute = ElementNames.RecordTypeNameAttribute;
            RecordTypePathAttribute = ElementNames.RecordTypePathAttribute;
            VarGroupElement = @namespace + ElementNames.VarGroupElement;
            VarGroupNameAttribute = ElementNames.VarGroupNameAttribute;
            VarGroupSystemAttribute = ElementNames.VarGroupSystemAttribute;
            InheritGroupElement = @namespace + ElementNames.InheritGroupElement;
            InheritElement = @namespace + ElementNames.InheritElement;
            InheritRecordTypeNameAttribute = ElementNames.InheritRecordTypeNameAttribute;
            ExpressionVariableGroupElement = @namespace + ElementNames.ExpressionVariableGroupElement;
            ExpressionVariableElement = @namespace + ElementNames.ExpressionVariableElement;
            ExpressionVariableNameAttribute = ElementNames.ExpressionVariableNameAttribute;
            DocumentationElement = @namespace + ElementNames.DocumentationElement;
            FieldElement = @namespace + ElementNames.FieldElement;
            FieldNameAttribute = ElementNames.FieldNameAttribute;
            FieldVarClassAttribute = ElementNames.FieldVarClassAttribute;
            FieldVarTypeAttribute = ElementNames.FieldVarTypeAttribute;
            FieldVarValueAttribute = ElementNames.FieldVarValueAttribute;
            FieldVarDefaultValueAttribute = ElementNames.FieldVarDefaultValueAttribute;

            FieldPropertyValueAttribute = ElementNames.FieldPropertyValueAttribute;

            FieldVarClassElement = @namespace + ElementNames.FieldVarClassElement;
            FieldVarTypeElement = @namespace + ElementNames.FieldVarTypeElement;
            FieldVarValueElement = @namespace + ElementNames.FieldVarValueElement;
            FieldVarDefaultValueElement = @namespace + ElementNames.FieldVarDefaultValueElement;
            FieldArzValueTypeElement = @namespace + ElementNames.FieldArzValueTypeElement;
            FieldArrayElement = @namespace + ElementNames.FieldArrayElement;
        }
    }
}
