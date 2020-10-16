using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Metadata
{
    using Builder;

    internal interface IRecordTypeBuilderContract : IRecordTypeSharedContract<
            MetadataBuilder,
            RecordTypeBuilder,
            FieldTypeBuilder,
            ExpressionVariableBuilder,
            FieldVarGroupBuilder
            >
    { }

    internal interface IRecordTypeContract : IRecordTypeSharedContract<
            DatabaseType,
            RecordType,
            FieldType,
            ExpressionVariable,
            FieldVarGroup>
    {
        /// <summary>
        /// Gets the fully qualified name of the record type.
        /// This fully-qualified template name, and can be used as value in 
        /// <see cref="IRecordTypeResolver"/>.
        /// Record have this field implicitly defined.
        /// </summary>
        Path TemplateName { get; }

        /// <summary>Record class, as it defined by field.</summary>
        /// <remarks>Record may define field with empty class value, or if not defined, this property will also be empty.</remarks>
        string Class { get; }

        IEnumerable<RecordType> InheritedRecordTypes { get; }

        /// <summary>A collection of the fields defined by the current record type.</summary>
        IEnumerable<FieldType> DeclaredFieldTypes { get; }

        IEnumerable<ExpressionVariable> DeclaredExpressionVariables { get; }

        /// <summary>
        /// Returns all fields of the current <see cref="IRecordType"/>.
        /// </summary>
        /// <remarks>This method returns all fields specific for current record type.
        /// Fields from inherited record types are also returned, but filtered by using
        /// inheritance rules (e.g. only fields with unique names are returned).
        /// </remarks>
        IEnumerable<FieldType> GetFields();

        FieldType GetField(string name);

        IEnumerable<ExpressionVariable> GetExpressionVariables();
        bool TryGetExpressionVariable(string name,
            [NotNullWhen(returnValue: true)] out ExpressionVariable? result);
        ExpressionVariable GetExpressionVariable(string name);
    }

    internal interface IRecordTypeSharedContract
        <TDatabaseType, TRecordType, TFieldType, TExpressionVariable, TFieldVarGroup>
        where TDatabaseType : class
        where TRecordType : class
        where TFieldType : class
        where TExpressionVariable : class
        where TFieldVarGroup : class
    {
        /// <summary>Gets the name of the record type.</summary>
        string Name { get; }

        TDatabaseType DeclaringDatabaseType { get; }

        bool TryGetField(string name, [NotNullWhen(true)] out TFieldType? result);
    }
}
