using System.Linq;

using Glacie.Data.Tpl;

namespace Glacie.Targeting
{
    public class TitanQuestAnniversaryEditionTarget : Target
    {
        protected override IVirtualPathMapper? CreateTemplateNameMapper()
        {
            return new TqaeTemplateNameMapper();
        }

        protected override ITemplateProcessor? CreateTemplateProcessor()
        {
            return new TqaeTemplateProcessor();
        }

        private sealed class TqaeTemplateNameMapper : IVirtualPathMapper
        {
            public VirtualPath Map(in VirtualPath path)
            {
                return path
                    .TrimStart("%template_dir%", VirtualPathComparison.NonStandard)
                    .TrimStartSegment("custommaps/art_tqx3", VirtualPathComparison.NonStandard);
            }
        }

        private sealed class TqaeTemplateProcessor : ITemplateProcessor
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
                                    "file_dbrr",
                                    "file_dbr");
                            }
                        }
                        break;

                    case "database/templates/actorhideable.tpl":
                        {
                            var actorVariablesGroup = template.Root
                                ?.Group("Actor Variables");
                            if (actorVariablesGroup != null)
                            {
                                FixGroupType(actorVariablesGroup, "lsit", "list");
                            }
                        }
                        break;

                    case "database/templates/casinomerchantconf.tpl":
                        {
                            var orbsGroup = template.Root
                                ?.Group("Casino Conf")
                                ?.Group("Orbs");
                            if (orbsGroup != null)
                            {
                                FixVariableClass(orbsGroup.Variable("NormalOrb"), "string", "variable");
                                FixVariableClass(orbsGroup.Variable("EpicOrb"), "string", "variable");
                                FixVariableClass(orbsGroup.Variable("LegendaryOrb"), "string", "variable");
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
