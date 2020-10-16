using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Metadata.Builder
{
    using ExpressionVariableBuilderMap = Dictionary<string, ExpressionVariableBuilder>;
    using FieldTypeBuilderMap = Dictionary<string, FieldTypeBuilder>;

    public sealed class RecordTypeBuilder
        : Infrastructure.Builder<RecordType>
        , IRecordTypeBuilderContract
    {
        private readonly MetadataBuilder _declaringDatabaseType;
        private readonly string _name;
        private Path _path;
        private bool _defined;

        private readonly FieldTypeBuilderMap _fieldTypeMap;
        private List<RecordTypeBuilder>? _inherits;
        private ExpressionVariableBuilderMap? _expressionVariableMap;

        private string? _documentation;

        internal RecordTypeBuilder(MetadataBuilder declaringDatabaseType, string name, bool defined)
        {
            Check.Argument.NotNull(declaringDatabaseType, nameof(declaringDatabaseType));
            Check.Argument.NotNull(name, nameof(name));

            _declaringDatabaseType = declaringDatabaseType;
            _name = name;
            _defined = defined;

            _fieldTypeMap = new FieldTypeBuilderMap(StringComparer.Ordinal);
        }

        public MetadataBuilder DeclaringDatabaseType => _declaringDatabaseType;

        public string Name => _name;

        public Path Path
        {
            get => _path;
            set
            {
                ThrowIfBuilt();
                _path = _declaringDatabaseType.NormalizePath(value);
            }
        }

        public string? Documentation
        {
            get => _documentation;
            set { ThrowIfBuilt(); _documentation = value; }
        }

        // TODO: TemplateName
        // TODO: Class

        public void AddInheritedFrom(RecordTypeBuilder value)
        {
            ThrowIfBuilt();

            if (_inherits == null) _inherits = new List<RecordTypeBuilder>();
            if (_inherits.Contains(value))
            {
                throw Error.InvalidOperation("Record definition already inherited from given record definition.");
            }
            _inherits.Add(value);
        }

        public ExpressionVariableBuilder DefineExpressionVariable(string name)
        {
            ThrowIfBuilt();

            if (_expressionVariableMap == null)
            {
                _expressionVariableMap = new ExpressionVariableBuilderMap(StringComparer.Ordinal);
            }

            var result = new ExpressionVariableBuilder(this, name);
            _expressionVariableMap.Add(result.Name, result);
            return result;
        }

        public FieldTypeBuilder DefineField(string name, FieldVarGroupBuilder? fieldVarGroup = null)
        {
            ThrowIfBuilt();

            var result = new FieldTypeBuilder(this, name);
            if (fieldVarGroup != null)
            {
                result.FieldVarGroup = fieldVarGroup;
            }
            if (!_fieldTypeMap.TryAdd(result.Name, result))
            {
                throw Error.InvalidOperation("Field with same name \"{0}\" already defined. Record type: \"{1}\".",
                    result.Name, Name);
            }
            return result;
        }

        public bool TryGetField(string name, [NotNullWhen(returnValue: true)] out FieldTypeBuilder? result)
        {
            return _fieldTypeMap.TryGetValue(name, out result);
        }

        internal bool IsDefined => _defined;

        internal void Define() => _defined = true;

        protected override RecordType BuildCore()
        {
            if (!IsDefined) throw Error.InvalidOperation("RecordType \"{0}\" is referenced without definition.", Name);

            var result = new RecordType(_name, _path);

            foreach (var x in _fieldTypeMap.Values)
            {
                result.Add(x.Build());
            }

            if (_expressionVariableMap != null)
            {
                foreach (var x in _expressionVariableMap.Values)
                {
                    result.Add(x.Build());
                }
            }

            // TODO: Block recursion.
            if (_inherits != null)
            {
                foreach (var x in _inherits)
                {
                    result.InheritForm(x.Build());
                }
            }

            return result;
        }
    }
}
