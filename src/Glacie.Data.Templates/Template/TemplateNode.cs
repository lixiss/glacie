using Glacie.Diagnostics;

namespace Glacie.Data.Templates
{
    public abstract class TemplateNode
    {
        protected TemplateNode() { }

        public string Name { get; set; } = default!;

        // TODO: Report locations.
        public Location GetLocation() => Location.None;
    }
}
