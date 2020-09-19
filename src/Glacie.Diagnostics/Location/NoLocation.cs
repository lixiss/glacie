using System.Text;

namespace Glacie.Diagnostics
{
    internal sealed class NoLocation : Location
    {
        private readonly static NoLocation s_instance = new NoLocation();

        public static NoLocation Instance => s_instance;

        private NoLocation() { }

        public override LocationKind Kind => LocationKind.None;

        public override string ToString()
        {
            return "";
        }

        internal override void FormatTo(StringBuilder builder) { }
    }
}
