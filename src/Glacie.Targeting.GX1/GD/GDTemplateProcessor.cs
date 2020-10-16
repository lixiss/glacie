using Glacie.Data.Templates;
using Glacie.Targeting.Generic;

namespace Glacie.Targeting.GD
{
    internal sealed class GDTemplateProcessor : GenericTemplateProcessor
    {
        public override void ProcessTemplate(Template template)
        {
            base.ProcessTemplate(template);

            if (template.Path.Equals("database/templates/fixeditemshrine.tpl", Path1Comparison.OrdinalIgnoreCase))
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
        }
    }
}
