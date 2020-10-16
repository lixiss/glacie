using Glacie.Data.Arz;

namespace Glacie.Metadata
{
    using Builder;

    internal interface IFieldTypeBuilderContract : IFieldTypeSharedContract<
            MetadataBuilder,
            RecordTypeBuilder,
            FieldTypeBuilder,
            ExpressionVariableBuilder,
            FieldVarGroupBuilder,
            ArzValueType?,
            bool?
            >
    { }

    internal interface IFieldTypeContract : IFieldTypeSharedContract<
            DatabaseType,
            RecordType,
            FieldType,
            ExpressionVariable,
            FieldVarGroup,
            ArzValueType,
            bool>
    { }


    internal interface IFieldTypeSharedContract
        <TDatabaseType,
        TRecordType,
        TFieldType,
        TExpressionVariable,
        TFieldVarGroup,
        TArzValueType,
        TBoolean>
        where TDatabaseType : class
        where TRecordType : class
        where TFieldType : class
        where TExpressionVariable : class
        where TFieldVarGroup : class
        // where TArzValueType : ArzValueType
    {
        /// <summary>Associated information field group.</summary>
        TFieldVarGroup FieldVarGroup { get; }

        /// <summary>Record type declared this field.</summary>
        TRecordType DeclaringRecordType { get; }

        /// <summary>Field name, as it stored in database.</summary>
        string Name { get; }

        /// <summary>Informational description.</summary>
        string? Documentation { get; }

        /// <summary>Variable class as it defined by Template.</summary>
        string VarClass { get; }

        /// <summary>Variable type as it defined by Template.</summary>
        string VarType { get; }

        /// <summary>Variable value as it defined by Template.</summary>
        string? VarValue { get; }

        /// <summary>Variable default value as it defined by Template.</summary>
        string? VarDefaultValue { get; }


        // TODO: ValueType -> RawValueType

        /// <summary>Field value type used by ARZ database. Always defined.</summary>
        TArzValueType ValueType { get; }

        /// <summary>Indicates if field may have multiple values, or should have single value.</summary>
        TBoolean Array { get; }
    }
}
