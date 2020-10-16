using System.Text;

namespace Glacie.Diagnostics
{
    public sealed class RecordFieldLocation : Location
    {
        private readonly string _recordName;
        private readonly string _fieldName;

        internal RecordFieldLocation(string recordName, string fieldName)
        {
            _recordName = recordName;
            _fieldName = fieldName;
        }

        public string RecordName => _recordName;

        public string FieldName => _fieldName;

        public override LocationKind Kind => LocationKind.RecordField;

        public override string ToString()
        {
            return _recordName + "(" + _fieldName + ")";
        }

        internal override void FormatTo(StringBuilder builder)
        {
            builder.Append(_recordName);
            builder.Append('(');
            builder.Append(_fieldName);
            builder.Append(')');
        }
    }
}
