using System;

using Glacie.Data.Arz;

namespace Glacie.Data.Metadata.V1
{
    public sealed class FieldType
    {
        private readonly string _name;
        private readonly string? _description;

        private readonly ArzValueType _valueType;
        private readonly bool _array;

        private readonly string _varClass;
        private readonly string _varType;
        private readonly string? _varValue;
        private readonly string? _varDefaultValue;

        private readonly FieldGroupDefinition _fieldGroupDefinition;

        private RecordType? _declaringRecordDefinition;

        #region Construction

        internal FieldType(string name,
            string? description,
            ArzValueType valueType,
            bool array,
            string varClass,
            string varType,
            string? varValue,
            string? varDefaultValue,
            FieldGroupDefinition fieldGroupDefinition)
        {
            _name = name;
            _description = description;

            _valueType = valueType;
            _array = array;

            _varClass = varClass;
            _varType = varType;
            _varValue = varValue;
            _varDefaultValue = varDefaultValue;

            _fieldGroupDefinition = fieldGroupDefinition;

            // TODO: Where to validate property consistency?
        }

        internal void AttachTo(RecordType declaringRecordDefinition)
        {
            Check.That(_declaringRecordDefinition == null);
            _declaringRecordDefinition = declaringRecordDefinition;
        }

        #endregion

        public string Name => _name;

        public FieldGroupDefinition FieldGroupDefinition => _fieldGroupDefinition;

        public RecordType DeclaringRecordDefinition => _declaringRecordDefinition!;

        public string? Description => _description;

        public ArzValueType ValueType => _valueType;

        public bool Array => _array;

        // E.g. should specify:
        // Primitive(s): int/real/string/bool
        // Expression: "equation" -> mapped to string ValueType == string.
        // ResourceReference(s): (common)
        // file_dbr:
        //"file_dbr" => ArzValueType.String,
        //"file_tex" => ArzValueType.String,
        //"file_msh" => ArzValueType.String,
        //"file_ssh" => ArzValueType.String,
        //"file_anm" => ArzValueType.String,
        //"file_qst" => ArzValueType.String,
        //"file_fnt" => ArzValueType.String,
        //"file_pfx" => ArzValueType.String,
        //"file_wav,mp3" => ArzValueType.String,
        // TQAE: "file_mp3,wav" => ArzValueType.String,
        // GD: 
        //"file_cnv" => ArzValueType.String,
        //"file_snd" => ArzValueType.String,
        //"file_wav,ogg" => ArzValueType.String,
        //"file_lua" => ArzValueType.String,
        //"file_txt" => ArzValueType.String,

        // FieldValueClassKind ValueClass => _classKind;

        // public bool Required => _required;
        // public Variant RequiredValue => _requiredValue;

        // Var* properties is specify TPL's Variable properties.
        public string VarClass => _varClass;
        public string VarType => _varType;
        public string? VarValue => _varValue;
        public string? VarDefaultValue => _varDefaultValue;

        // TODO: May be make "Class" as special property in RecordDefinition

        // TODO: ArzValueType ValueType { get; set; }
        // TODO: Valid Value Range (some float32 values should be in 0..100 range, see description [0..100]

    }
}
