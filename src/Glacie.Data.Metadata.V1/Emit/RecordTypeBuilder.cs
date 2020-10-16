using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Data.Metadata.V1.Emit
{
    using ExpressionVariableDefinitionMap = Dictionary<string, ExpressionVariableDeclarationBuilder>;
    using FieldDefinitionMap = Dictionary<string, FieldTypeBuilder>;

    // TODO: Symbolized builder?
    // TODO: Builders should have similar API as Definitions API (in terms of reading, and allow more read/write options)
    // This practically allow to read definitions, then patch them and use. 

    public sealed class RecordTypeBuilder
    {
        private RecordType? _built;

        private readonly DatabaseTypeBuilder _declaringDatabaseDefinition;
        private readonly string _name;
        private readonly FieldDefinitionMap _fieldDefinitionMap;
        private List<RecordTypeBuilder>? _inherits;
        private ExpressionVariableDefinitionMap? _expressionVariableBuilderMap;

        internal RecordTypeBuilder(DatabaseTypeBuilder declaringDatabaseDefinition, string name)
        {
            _declaringDatabaseDefinition = declaringDatabaseDefinition;
            _name = name;

            _fieldDefinitionMap = new FieldDefinitionMap(StringComparer.Ordinal);
        }

        public DatabaseTypeBuilder DeclaringDatabaseDefinition => _declaringDatabaseDefinition;

        public string Name => _name;

        public void AddInheritedFrom(RecordTypeBuilder value)
        {
            if (_inherits == null) _inherits = new List<RecordTypeBuilder>();
            if (_inherits.Contains(value))
            {
                throw Error.InvalidOperation("Record definition already inherited from given record definition.");
            }
            _inherits.Add(value);

            // TODO: For code generation we probably want to calculate which definition is used
            // as base definition, also would be good to determine which definitions are "final"
            // (e.g. actually might be used by database) - difference what from some definitions
            // we might create new records, but from fully abstract - we can't.
            // also abstract definitions actually doesn't need to define some fields.
            // However this information is static, and should be collected over existing
            // database / specified manually / etc.
            // 
            // value.UsedAsBaseDefinition = true;
            //
            // however if do it, it is better to do it as separate step, otherwise results might be inconsistent.
        }

        public ExpressionVariableDeclarationBuilder DefineExpressionVariableDefinition(string name)
        {
            if (_expressionVariableBuilderMap == null)
            {
                _expressionVariableBuilderMap = new ExpressionVariableDefinitionMap(StringComparer.Ordinal);
            }

            var result = new ExpressionVariableDeclarationBuilder(this, name);
            _expressionVariableBuilderMap.Add(result.Name, result);
            return result;
        }

        public FieldTypeBuilder DefineFieldDefinition(FieldGroupBuilder fieldGroupDefinition, string name)
        {
            var result = new FieldTypeBuilder(this, fieldGroupDefinition, name);
            _fieldDefinitionMap.Add(result.Name, result);
            return result;
        }

        public bool TryGetFieldType(string name, [NotNullWhen(returnValue: true)] out FieldTypeBuilder? result)
        {
            return _fieldDefinitionMap.TryGetValue(name, out result);
        }

        public RecordType CreateRecordType()
        {
            // Intended behavior what RecordType accessed without DatabaseType been exposed,
            // and must be cached.
            if (_built != null) return _built;

            var recordDefinition = new RecordType(_name);

            foreach (var x in _fieldDefinitionMap.Values)
            {
                recordDefinition.Add(x.CreateFieldType());
            }

            if (_expressionVariableBuilderMap != null)
            {
                foreach (var x in _expressionVariableBuilderMap.Values)
                {
                    recordDefinition.Add(x.Build());
                }
            }

            if (_inherits != null)
            {
                foreach (var x in _inherits)
                {
                    recordDefinition.InheritForm(x.CreateRecordType());
                }
            }

            return _built = recordDefinition;
        }
    }
}
