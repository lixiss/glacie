using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Glacie.Data.Metadata
{
    using ExpressionVariableDefinitionMap = Dictionary<string, ExpressionVariableDeclaration>;
    using FieldDefinitionMap = Dictionary<string, FieldType>;

    public sealed class RecordType
    {
        private readonly string _name;
        private readonly FieldDefinitionMap _declaredFieldDefinitionMap;
        private ExpressionVariableDefinitionMap? _expressionVariableDefinitionMap;
        private List<RecordType>? _inheritedRecordDefinitions;

        private DatabaseType? _declaringDatabaseDefinition;

        #region Construction

        internal RecordType(string name)
        {
            _name = name;
            _declaredFieldDefinitionMap = new FieldDefinitionMap(StringComparer.Ordinal);
        }

        internal void Add(FieldType value)
        {
            _declaredFieldDefinitionMap.Add(value.Name, value);
            value.AttachTo(this);
        }

        internal void Add(ExpressionVariableDeclaration value)
        {
            if (_expressionVariableDefinitionMap == null)
            {
                _expressionVariableDefinitionMap = new ExpressionVariableDefinitionMap(StringComparer.Ordinal);
            }

            _expressionVariableDefinitionMap.Add(value.Name, value);
            value.AttachTo(this);
        }

        internal void InheritForm(RecordType baseRecordDefinition)
        {
            if (_inheritedRecordDefinitions == null)
            {
                _inheritedRecordDefinitions = new List<RecordType>();
            }
            _inheritedRecordDefinitions.Add(baseRecordDefinition);
        }

        internal void AttachTo(DatabaseType declaringDatabaseDefinition)
        {
            // TODO: add "virtual" templateName field.

            Check.That(_declaringDatabaseDefinition == null);
            _declaringDatabaseDefinition = declaringDatabaseDefinition;
        }

        #endregion

        public DatabaseType DeclaringDatabaseType => _declaringDatabaseDefinition!;

        /// <summary>
        /// Gets the name of the record definition.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the fully qualified name of the record definition.
        /// This name is value which can be used as "templateName" value.
        /// </remarks>
        public string FullName => throw Error.NotImplemented();

        public IEnumerable<ExpressionVariableDeclaration> DeclaredExpressionVariables
            => _expressionVariableDefinitionMap?.Values ?? Enumerable.Empty<ExpressionVariableDeclaration>();

        public IEnumerable<ExpressionVariableDeclaration> GetExpressionVariables()
            => throw Error.NotImplemented();

        public ExpressionVariableDeclaration GetExpressionVariable(string name)
            => throw Error.NotImplemented();

        public bool TryGetExpressionVariable(string name,
            [NotNullWhen(returnValue: true)] out ExpressionVariableDeclaration? result)
            => throw Error.NotImplemented();

        public ExpressionVariableDeclaration GetExpressionVariable(NameSymbol name)
            => throw Error.NotImplemented();

        public bool TryGetExpressionVariable(NameSymbol name,
            [NotNullWhen(returnValue: true)] out ExpressionVariableDeclaration? result)
            => throw Error.NotImplemented();


        /// <summary>
        /// Gets all record definitions inherited by the current <see cref="RecordType"/>.
        /// </summary>
        public IEnumerable<RecordType> InheritedRecordDefinitions
            => _inheritedRecordDefinitions ?? Enumerable.Empty<RecordType>();

        /// <summary>
        /// A collection of the fields defined by the current record definition.
        /// </summary>
        public IEnumerable<FieldType> DeclaredFieldDefinitions
            => _declaredFieldDefinitionMap.Values;

        /// <summary>
        /// Returns all the public fields of the current record definition.
        /// </summary>
        public IEnumerable<FieldType> GetFields()
            => throw Error.NotImplemented();

        /// <summary>
        /// Searches for the field with the specified name.
        /// </summary>
        public FieldType GetField(string name)
        {
            if (TryGetField(name, out var fieldType))
            {
                return fieldType;
            }
            else throw Error.InvalidOperation("Field \"{0}\" not found.", name); // TODO: better message
        }

        /// <summary>
        /// Searches for the field with the specified name.
        /// </summary>
        public bool TryGetField(string name,
            [NotNullWhen(true)] out FieldType? result)
        {
            // TODO: build field map instead of dynamic lookup?
            if (_declaredFieldDefinitionMap.TryGetValue(name, out result))
            {
                return true;
            }

            if (_inheritedRecordDefinitions != null)
            {
                foreach (var inheritedRecordType in _inheritedRecordDefinitions)
                {
                    if (inheritedRecordType.TryGetField(name, out result)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Searches for the field with the specified name symbol.
        /// </summary>
        public FieldType GetField(RecordFieldNameSymbol nameSymbol)
            => throw Error.NotImplemented();

        /// <summary>
        /// Searches for the field with the specified name symbol.
        /// </summary>
        public bool TryGetField(RecordFieldNameSymbol nameSymbol,
            [NotNullWhen(true)] out FieldType? result)
            => throw Error.NotImplemented();
    }
}
