using System.Text;

namespace Glacie.Diagnostics
{
    public sealed class FileLocation : Location
    {
        private readonly string? _path;
        private readonly int _lineNumber;
        private readonly int _linePosition;

        internal FileLocation(string? path, int lineNumber, int linePosition)
        {
            _path = path;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        public override LocationKind Kind => LocationKind.File;

        public override string ToString()
        {
            return _path + "(" + _lineNumber + "," + _linePosition + ")";
        }

        internal override void FormatTo(StringBuilder builder)
        {
            builder.Append(_path);
            builder.Append('(');
            builder.Append(_lineNumber);
            builder.Append(',');
            builder.Append(_linePosition);
            builder.Append(')');
        }
    }
}