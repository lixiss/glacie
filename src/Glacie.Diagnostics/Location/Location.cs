using System.Text;

namespace Glacie.Diagnostics
{
    public abstract class Location
    {
        public static Location None => NoLocation.Instance;

        public static Location Create(string? path, int lineNumber, int linePosition)
        {
            return new FileLocation(path, lineNumber, linePosition);
        }

        public abstract LocationKind Kind { get; }

        public abstract override string ToString();

        internal abstract void FormatTo(StringBuilder builder);
    }
}
