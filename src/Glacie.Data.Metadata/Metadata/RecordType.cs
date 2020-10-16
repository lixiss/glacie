using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Glacie.Metadata
{
    // TODO: Can we use "symbolized" lookup / string table to perform lookups?

    using ExpressionVariableMap = Dictionary<string, ExpressionVariable>;
    using FieldDefinitionMap = Dictionary<string, FieldType>;

    /// <inheritdoc cref="IRecordTypeContract"/>
    public sealed class RecordType : IRecordTypeContract
    {
        private readonly string _name;
        private readonly Path _path;
        private Path _templateName; // filled on attach

        private readonly string _class;

        private readonly FieldDefinitionMap _declaredFieldTypeMap;
        private ExpressionVariableMap? _declaredExpressionVariableMap;
        private List<RecordType>? _inheritedRecordTypes;

        private DatabaseType? _declaringDatabaseType;

        #region Construction

        internal RecordType(string name, Path path)
        {
            _name = name;
            _path = path;
            _declaredFieldTypeMap = new FieldDefinitionMap(StringComparer.Ordinal);
        }

        internal void Add(FieldType value)
        {
            _declaredFieldTypeMap.Add(value.Name, value);
            value.AttachTo(this);
        }

        internal void Add(ExpressionVariable value)
        {
            if (_declaredExpressionVariableMap == null)
            {
                _declaredExpressionVariableMap = new ExpressionVariableMap(StringComparer.Ordinal);
            }

            _declaredExpressionVariableMap.Add(value.Name, value);
            value.AttachTo(this);
        }

        internal void InheritForm(RecordType baseRecordDefinition)
        {
            if (_inheritedRecordTypes == null)
            {
                _inheritedRecordTypes = new List<RecordType>();
            }
            _inheritedRecordTypes.Add(baseRecordDefinition);
        }

        internal void AttachTo(DatabaseType declaringDatabaseType)
        {
            // TODO: add "virtual" templateName field.

            Check.That(_declaringDatabaseType == null);
            _declaringDatabaseType = declaringDatabaseType;

            DebugCheck.That(_templateName.IsEmpty);
            _templateName = Path.Join(declaringDatabaseType.BasePath, _path);
        }

        #endregion

        public string Name => _name;

        public Path Path => _path;

        public Path TemplateName => _templateName;

        public string Class => _class;

        public DatabaseType DeclaringDatabaseType => _declaringDatabaseType!;

        public IEnumerable<RecordType> InheritedRecordTypes
            => _inheritedRecordTypes ?? Enumerable.Empty<RecordType>();

        public IEnumerable<FieldType> DeclaredFieldTypes
            => _declaredFieldTypeMap.Values;

        public IEnumerable<ExpressionVariable> DeclaredExpressionVariables
            => _declaredExpressionVariableMap?.Values ?? Enumerable.Empty<ExpressionVariable>();

        public IEnumerable<FieldType> GetFields()
            => throw Error.NotImplemented();

        public bool TryGetField(string name,
            [NotNullWhen(true)] out FieldType? result)
        {
            // TODO: build field map instead of dynamic lookup?
            if (_declaredFieldTypeMap.TryGetValue(name, out result))
            {
                return true;
            }

            if (_inheritedRecordTypes != null)
            {
                foreach (var inheritedRecordType in _inheritedRecordTypes)
                {
                    if (inheritedRecordType.TryGetField(name, out result)) return true;
                }
            }

            return false;
        }

        public FieldType GetField(string name)
        {
            if (TryGetField(name, out var fieldType))
            {
                return fieldType;
            }
            else throw Error.InvalidOperation("Field \"{0}\" not found.", name); // TODO: better message
        }

        public IEnumerable<ExpressionVariable> GetExpressionVariables()
            => throw Error.NotImplemented();

        public bool TryGetExpressionVariable(string name,
            [NotNullWhen(returnValue: true)] out ExpressionVariable? result)
            => throw Error.NotImplemented();

        public ExpressionVariable GetExpressionVariable(string name)
            => throw Error.NotImplemented();
    }
}
