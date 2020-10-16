namespace Glacie.Metadata
{
    using Builder;

    internal interface IExpressionVariableBuilderContract : IExpressionVariableSharedContract<
            MetadataBuilder,
            RecordTypeBuilder,
            FieldTypeBuilder,
            ExpressionVariableBuilder,
            FieldVarGroupBuilder
            >
    { }

    internal interface IExpressionVariableContract : IExpressionVariableSharedContract<
            DatabaseType,
            RecordType,
            FieldType,
            ExpressionVariable,
            FieldVarGroup>
    { }

    internal interface IExpressionVariableSharedContract
        <TDatabaseType, TRecordType, TFieldType, TExpressionVariable, TFieldVarGroup>
        where TDatabaseType : class
        where TRecordType : class
        where TFieldType : class
        where TExpressionVariable : class
        where TFieldVarGroup : class
    {
        TRecordType DeclaringRecordType { get; }

        string Name { get; }

        string? Documentation { get; }
    }
}
