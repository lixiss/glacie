using System;
using System.Linq;

using Glacie.Data.Templates;

namespace Glacie.Targeting.Generic
{
    public class GenericTemplateProcessor : ITemplateProcessor
    {
        public virtual void ProcessTemplate(Template template)
        {
            // Template's include directives, sometimes contains %TEMPLATE_DIR%
            // environment variable. We are strictly remove all them.

            const string templateDirPrefix = "%TEMPLATE_DIR%";

            var includeVariables = template.Descendants()
                .OfType<TemplateVariable>()
                .Where(x => "include".Equals(x.Type, StringComparison.OrdinalIgnoreCase)
                    && "static".Equals(x.Class, StringComparison.OrdinalIgnoreCase)
                    && "Include File".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
            foreach (var includeVariable in includeVariables)
            {
                var value = includeVariable.DefaultValue;
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.StartsWith(templateDirPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        includeVariable.DefaultValue = value.Substring(templateDirPrefix.Length);
                    }
                }
            }
        }

        protected internal static void FixGroupType(TemplateGroup? group, string oldValue, string newValue)
        {
            if (group == null) return;

            if (group.Type == oldValue) group.Type = newValue;
        }

        protected internal static void FixVariableType(TemplateVariable? variable, string oldValue, string newValue)
        {
            if (variable == null) return;

            if (variable.Type == oldValue) variable.Type = newValue;
        }

        protected internal static void FixVariableClass(TemplateVariable? variable, string oldValue, string newValue)
        {
            if (variable == null) return;

            if (variable.Class == oldValue) variable.Class = newValue;
        }
    }
}
