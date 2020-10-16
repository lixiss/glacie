using System;
using System.Collections.Generic;

namespace Glacie.Targeting.TQAE
{
    internal static class TqaeTemplateNameExcludeList
    {
        private static readonly HashSet<string> s_list = new HashSet<string>(PathComparer.OrdinalIgnoreCase)
        {
            "database/templates/ingameui/copy of bitmapsingle.tpl",
        };

        public static bool IsExcluded(Path templateName)
        {
            return s_list.TryGetValue(templateName.ToString(), out var _);
        }
    }
}
