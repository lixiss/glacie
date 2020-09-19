using Glacie.Data.Tpl;

namespace Glacie.Targeting
{
    public class GrimDawnTarget : Target
    {
        protected override IVirtualPathMapper? CreateTemplateNameMapper()
        {
            return new GDTemplateNameMapper();
        }

        protected override ITemplateProcessor? CreateTemplateProcessor()
        {
            return new GDTemplateProcessor();
        }

        // TODO: suppress templates, because they can't be parsed (and they not used anyway):
        // "database/templates/copy of lootitemtable_dynweightdynaffix.tpl"
        // "database/templates/copy of lootitemtable_dynweighted_dynaffix.tpl"

        private sealed class GDTemplateNameMapper : IVirtualPathMapper
        {
            public VirtualPath Map(in VirtualPath path)
            {
                return path
                    .TrimStart("%template_dir%", VirtualPathComparison.NonStandard);
            }
        }

        private sealed class GDTemplateProcessor : ITemplateProcessor
        {
            public void ProcessTemplate(Template template)
            {
                switch (template.Name)
                {
                    case "database/templates/fixeditemshrine.tpl":
                        {
                            var shrineSoundsGroup = template.Root
                                ?.Group("Shrine Sounds");
                            if (shrineSoundsGroup != null)
                            {
                                FixVariableType(shrineSoundsGroup.Variable("idleSound"),
                                    "***UNKNOWN***",
                                    "file_dbr");
                            }
                        }
                        break;
                }
            }

            private static void FixGroupType(TemplateGroup? group, string oldValue, string newValue)
            {
                if (group == null) return;

                if (group.Type == oldValue) group.Type = newValue;
            }

            private static void FixVariableType(TemplateVariable? variable, string oldValue, string newValue)
            {
                if (variable == null) return;

                if (variable.Type == oldValue) variable.Type = newValue;
            }

            private static void FixVariableClass(TemplateVariable? variable, string oldValue, string newValue)
            {
                if (variable == null) return;

                if (variable.Class == oldValue) variable.Class = newValue;
            }
        }
    }
}
