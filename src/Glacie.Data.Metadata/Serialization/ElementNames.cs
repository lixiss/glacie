namespace Glacie.Metadata.Serialization
{
    internal static class ElementNames
    {
        public const string MetadataNamespace = "urn:glacie:metadata:v1";
        public const string MetadataPatchNamespace = "urn:glacie:metadata:patch:v1";

        // Root Elements
        public const string MetadataRootElement = "metadata";
        public const string MetadataPatchRootElement = "metadata-patch";
        public const string MetadataPartialRootElement = "partial";
        public const string MetadataPatchPartialRootElement = MetadataPartialRootElement;
        public const string MetadataRootBasePathAttribute = "base-path";

        // Include Element
        public const string IncludeDirectiveElement = "include";
        public const string IncludeDirectivePathAttribute = "path";

        // RecordType
        public const string RecordTypeElement = "record-type";
        public const string RecordTypeNameAttribute = "name";
        public const string RecordTypePathAttribute = "path";

        // VarGroup
        public const string VarGroupElement = "var-group";
        public const string VarGroupNameAttribute = "name";
        public const string VarGroupSystemAttribute = "system";

        // Inheritance
        public const string InheritGroupElement = "inherits";
        public const string InheritElement = "inherit";
        public const string InheritRecordTypeNameAttribute = "record"; // TODO: record-type (name)

        // Expression Variable
        public const string ExpressionVariableGroupElement = "expression-variables";
        public const string ExpressionVariableElement = "expression-variable";
        public const string ExpressionVariableNameAttribute = "name";

        // Documentation element, appears under RecordType, FieldType and ExpressionVariable
        public const string DocumentationElement = "description"; // TODO: rename me

        // FieldType
        public const string FieldElement = "field";
        public const string FieldNameAttribute = "name";

        public const string FieldVarClassAttribute = "var-class";
        public const string FieldVarTypeAttribute = "var-type";
        public const string FieldVarValueAttribute = "var-value";
        public const string FieldVarDefaultValueAttribute = "var-default-value";

        // Field Properties
        public const string FieldVarClassElement = "var-class";
        public const string FieldVarTypeElement = "var-type";
        public const string FieldVarValueElement = "var-value";
        public const string FieldVarDefaultValueElement = "var-default-value";
        public const string FieldArzValueTypeElement = "arz-value-type";
        public const string FieldArrayElement = "array";

        // Properties, Shared
        public const string FieldPropertyValueAttribute = "value";
    }
}
