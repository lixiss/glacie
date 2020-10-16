using System.Text;

namespace Glacie.Diagnostics
{
    public sealed class RecordLocation : Location
    {
        private readonly string _recordName;

        internal RecordLocation(string recordName)
        {
            _recordName = recordName;
        }

        public string RecordName => _recordName;

        public override LocationKind Kind => LocationKind.Record;

        public override string ToString()
        {
            return _recordName;
        }

        internal override void FormatTo(StringBuilder builder)
        {
            builder.Append(_recordName);
        }
    }
}
