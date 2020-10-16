using Glacie.Data.Arz;

namespace Glacie.Metadata.Builder
{
    public sealed class FieldTypeBuilder
        : Infrastructure.Builder<FieldType>
        , IFieldTypeBuilderContract
    {
        private readonly RecordTypeBuilder _declaringRecordType;
        private FieldVarGroupBuilder? _fieldVarGroup;
        private readonly string _name;

        private string? _documentation;

        private string? _varClass;
        private string? _varType;
        private string? _varValue;
        private string? _varDefaultValue;
        private ArzValueType? _valueType;
        private bool? _array;


        internal FieldTypeBuilder(
            RecordTypeBuilder declaringRecordType,
            string name)
        {
            Check.Argument.NotNull(declaringRecordType, nameof(declaringRecordType));
            Check.Argument.NotNull(name, nameof(name));

            _declaringRecordType = declaringRecordType;
            _name = name;
        }

        public RecordTypeBuilder DeclaringRecordType => _declaringRecordType;

        public FieldVarGroupBuilder FieldVarGroup
        {
            get => _fieldVarGroup ?? DeclaringRecordType.DeclaringDatabaseType.RootFieldVarGroup;
            set
            {
                ThrowIfBuilt();

                if (value != _fieldVarGroup)
                {
                    if (value != null)
                    {
                        if (DeclaringRecordType.DeclaringDatabaseType != value.DeclaringDatabaseType)
                        {
                            throw Error.Argument(nameof(value), "Foreign fieldVarGroup");
                        }
                    }
                    _fieldVarGroup = value;
                }
            }
        }

        public string Name => _name;

        public string? Documentation
        {
            get => _documentation;
            set { ThrowIfBuilt(); _documentation = value; }
        }

        public string? VarClass
        {
            get => _varClass;
            set { ThrowIfBuilt(); _varClass = value; }
        }

        public string? VarType
        {
            get => _varType;
            set { ThrowIfBuilt(); _varType = value; }
        }

        public string? VarValue
        {
            get => _varValue;
            set { ThrowIfBuilt(); _varValue = value; }
        }

        public string? VarDefaultValue
        {
            get => _varDefaultValue;
            set { ThrowIfBuilt(); _varDefaultValue = value; }
        }

        public ArzValueType? ValueType
        {
            get => _valueType;
            set { ThrowIfBuilt(); _valueType = value; }
        }

        public bool? Array
        {
            get => _array;
            set { ThrowIfBuilt(); _array = value; }
        }

        protected override FieldType BuildCore()
        {
            // TODO: Emit proper diagnostics which should be bound to location / at least record type name.

            var varClass = VarClass;
            var varType = VarType;
            var varValue = VarValue;
            var varDefaultValue = VarDefaultValue;
            var valueType = ValueType;
            var array = Array;

            // Assign Defaults
            if (!valueType.HasValue)
            {
                valueType = varType switch
                {
                    "int" => ArzValueType.Integer,
                    "real" => ArzValueType.Real,
                    "string" => ArzValueType.String,
                    "bool" => ArzValueType.Boolean,

                    "equation" => ArzValueType.String,

                    // Common (TQAE+GD)
                    "file_dbr" => ArzValueType.String,
                    "file_tex" => ArzValueType.String,
                    "file_msh" => ArzValueType.String,
                    "file_ssh" => ArzValueType.String,
                    "file_anm" => ArzValueType.String,
                    "file_qst" => ArzValueType.String,
                    "file_fnt" => ArzValueType.String,
                    "file_pfx" => ArzValueType.String,
                    "file_wav,mp3" => ArzValueType.String,

                    // TQAE
                    "file_mp3,wav" => ArzValueType.String,

                    // GD
                    "file_cnv" => ArzValueType.String,
                    "file_snd" => ArzValueType.String,
                    "file_wav,ogg" => ArzValueType.String,
                    "file_lua" => ArzValueType.String,
                    "file_txt" => ArzValueType.String,

                    // TODO: Need apply special patches, there is only about shrine IdleSound
                    // doesn't want this values here
                    // "file_dbrr" => ArzValueType.String, // file_dbr - apply fixes, tqae...
                    // "***UNKNOWN***" => ArzValueType.String, // file_dbr - apply fixes, gd...

                    null => throw Error.InvalidOperation("Field type should specify ValueType or VarType"),
                    "" => throw Error.InvalidOperation("Field type should specify ValueType or VarType"),
                    _ => throw Error.InvalidOperation("Invalid VarType value: \"{0}\".", varType),
                };
            }

            if (!array.HasValue)
            {
                array = varClass switch
                {
                    "variable" => false,
                    "static" => false,
                    "picklist" => false,
                    "array" => true,

                    null => throw Error.InvalidOperation("Field type should specify Array or VarClass."),
                    "" => throw Error.InvalidOperation("Field type should specify Array or VarClass."),
                    _ => throw Error.InvalidOperation("Invalid VarClass value: \"{0}\".", varClass),
                };
            }

            if (string.IsNullOrEmpty(varClass))
            {
                varClass = array switch
                {
                    false => "variable",
                    true => "array",

                    _ => throw Error.InvalidOperation("Field type should specify Array or VarClass."),
                };
            }

            if (string.IsNullOrEmpty(varType))
            {
                varType = valueType switch
                {
                    ArzValueType.Integer => "int",
                    ArzValueType.Real => "real",
                    ArzValueType.String => "string",
                    ArzValueType.Boolean => "bool",

                    null => throw Error.InvalidOperation("Field type should specify ValueType or VarType."),
                    _ => throw Error.InvalidOperation("Invalid ValueType value: \"{0}\".", valueType),
                };
            }

            // TODO: parse static fields


            // Validate Consistency
            // TODO: ...
            Check.That(valueType != null);
            Check.That(array != null);
            Check.That(varClass != null);
            Check.That(varType != null);


            return new FieldType(
                name: _name,
                description: Documentation,
                valueType: valueType.Value,
                array: array.Value,
                varClass: varClass,
                varType: varType,
                varValue: varValue,
                varDefaultValue: varDefaultValue,
                fieldGroupDefinition: _fieldVarGroup.Build()
                );
        }
    }
}
