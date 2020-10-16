using System.Runtime.InteropServices.ComTypes;

using Glacie.Data.Arz;

namespace Glacie.Data.Metadata.V1.Emit
{
    // TODO: Make builders "freezable" and throw if frozen
    public sealed class FieldTypeBuilder
    {
        private FieldType? _built;

        private readonly RecordTypeBuilder _declaringRecordDefinition;
        private readonly FieldGroupBuilder _fieldGroupDefinition;
        private readonly string _name;

        internal FieldTypeBuilder(
            RecordTypeBuilder declaringRecordDefinition,
            FieldGroupBuilder fieldGroupDefinition,
            string name)
        {
            _declaringRecordDefinition = declaringRecordDefinition;
            _fieldGroupDefinition = fieldGroupDefinition;
            _name = name;
        }

        public RecordTypeBuilder DeclaringRecordDefinition => _declaringRecordDefinition;

        public FieldGroupBuilder FieldGroupDefinition => _fieldGroupDefinition;

        public string Name => _name;

        public string? Description { get; set; }

        // Properties below should reflect FieldDefinition's properties, but
        // might be nullable.
        public string? VarClass { get; set; }
        public string? VarType { get; set; }
        public string? VarValue { get; set; }
        public string? VarDefaultValue { get; set; }

        public ArzValueType? ValueType { get; set; }
        public bool? Array { get; set; }


        internal FieldType CreateFieldType()
        {
            // TODO: Emit proper diagnostics which should be bound to location / at least record type name.

            // Intended behavior what field is created only once.
            Check.That(_built == null);
            if (_built != null) return _built;

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


            return _built = new FieldType(
                name: _name,
                description: Description,
                valueType: valueType.Value,
                array: array.Value,
                varClass: varClass,
                varType: varType,
                varValue: varValue,
                varDefaultValue: varDefaultValue,
                fieldGroupDefinition: _fieldGroupDefinition.Build()
                );
        }
    }
}
