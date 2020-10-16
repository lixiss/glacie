namespace Glacie.Data.Templates
{
    public sealed class TemplateVariable : TemplateNode
    {
        public TemplateVariable() { }

        public string Class { get; set; } = default!;

        public string Type { get; set; } = default!;

        public string Description { get; set; } = default!;

        public string Value { get; set; } = default!;

        public string DefaultValue { get; set; } = default!;
    }
}
