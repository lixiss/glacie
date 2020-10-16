using System.Collections.Generic;

namespace Glacie.Metadata
{
    using Builder;

    internal interface IMetadataBuilderContract : IDatabaseTypeSharedContract<
            MetadataBuilder,
            RecordTypeBuilder,
            FieldTypeBuilder,
            ExpressionVariableBuilder,
            FieldVarGroupBuilder
            >
    { }

    internal interface IDatabaseTypeContract : IDatabaseTypeSharedContract<
            DatabaseType,
            RecordType,
            FieldType,
            ExpressionVariable,
            FieldVarGroup>
    { }

    internal interface IDatabaseTypeSharedContract
        <TDatabaseType, TRecordType, TFieldType, TExpressionVariable, TFieldVarGroup>
        where TDatabaseType : class
        where TRecordType : class
        where TFieldType : class
        where TExpressionVariable : class
        where TFieldVarGroup : class
    {
        TFieldVarGroup RootFieldVarGroup { get; }

        IReadOnlyCollection<TRecordType> RecordTypes { get; }
    }
}
