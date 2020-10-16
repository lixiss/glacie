using System;
using System.Text;

namespace Glacie.Diagnostics
{
    public abstract class Location
    {
        public static Location None => NoLocation.Instance;

        [Obsolete("There is should be another type of location without positions.")]
        public static FileLocation JustFile(string? path)
        {
            return new FileLocation(path, 0, 0);
        }

        public static FileLocation File(string? path, int lineNumber, int linePosition)
        {
            return new FileLocation(path, lineNumber, linePosition);
        }

        public static RecordLocation Record(string recordName)
        {
            return new RecordLocation(recordName);
        }

        public static RecordFieldLocation RecordField(string recordName, string fieldName)
        {
            return new RecordFieldLocation(recordName, fieldName);
        }

        public abstract LocationKind Kind { get; }

        public abstract override string ToString();

        internal abstract void FormatTo(StringBuilder builder);
    }
}
