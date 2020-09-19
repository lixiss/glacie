namespace Glacie.Data.Tpl
{
    public sealed class TemplateVariable : TemplateNode
    {
        public TemplateVariable() { }

        public string Class { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }

        public string Value { get; set; }

        public string DefaultValue { get; set; }
    }
}
