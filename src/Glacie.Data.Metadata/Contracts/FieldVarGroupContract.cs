using System.Collections.Generic;

namespace Glacie.Metadata
{
    using Builder;

    internal interface IFieldVarGroupBuilderContract : IFieldVarGroupSharedContract<
            MetadataBuilder,
            RecordTypeBuilder,
            FieldTypeBuilder,
            ExpressionVariableBuilder,
            FieldVarGroupBuilder
            >
    { }

    internal interface IFieldVarGroupContract : IFieldVarGroupSharedContract<
            DatabaseType,
            RecordType,
            FieldType,
            ExpressionVariable,
            FieldVarGroup>
    { }


    /// <summary>
    /// Informational field group, as it defined in templates.
    /// No special purpose, they can be completely ignored.
    /// Originally used for UI purposes.
    /// </summary>
    internal interface IFieldVarGroupSharedContract<TDatabaseType, TRecordType, TFieldType, TExpressionVariable, TFieldVarGroup>
        where TDatabaseType : class
        where TRecordType : class
        where TFieldType : class
        where TExpressionVariable : class
        where TFieldVarGroup : class
    {
        /// <summary>
        /// Unique group identifier in the <see cref="IDatabaseTypeContract"/> scope.
        /// </summary>
        int Id { get; }

        /// <summary>Group name. <see langword="null"/> for root group.</summary>
        string? Name { get; }

        /// <summary>Indicates what it is system group.</summary>
        /// <remarks>Header group usually has this flag set, but some other groups errorneously have this flag too.</remarks>
        bool System { get; }

        /// <summary>Parent group. Returns <see langword="null"/> for root group.</summary>
        TFieldVarGroup? Parent { get; }

        IEnumerable<TFieldVarGroup> Children { get; }

        TDatabaseType DeclaringDatabaseType { get; }

        // bool TryGetFieldVarGroup(string name, [NotNullWhen(returnValue: true)] out IFieldVarGroup? result);
    }
}
