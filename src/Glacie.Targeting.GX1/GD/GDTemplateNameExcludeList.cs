using System;
using System.Collections.Generic;

namespace Glacie.Targeting.GD
{
    internal static class GDTemplateNameExcludeList
    {
        private static readonly HashSet<string> s_list = new HashSet<string>(PathComparer.OrdinalIgnoreCase)
        {
            "database/templates/copy (2) of lootrandomizertable.tpl",
            "database/templates/copy of actor.tpl",
            "database/templates/copy of charanimationtable.tpl",
            "database/templates/copy of copy of copy of lootitemtable_dynweight.tpl",
            "database/templates/copy of copy of lootitemtable_dynweight.tpl",
            "database/templates/copy of lootitemtable_dynweight.tpl",
            "database/templates/copy of lootitemtable_dynweightdynaffix.tpl",
            "database/templates/copy of lootitemtable_dynweighted_dynaffix.tpl",
            "database/templates/copy of lootitemtable_fixedweight.tpl",
            "database/templates/copy of proxypool.tpl",
            "database/templates/copy of weapon_bow.tpl",
            "database/templates/backup/parametersoffensive.tpl",
            "database/templates/backup/parameters_character.tpl",
            "database/templates/backup/parameters_characterequation.tpl",
            "database/templates/backup/parameters_defensive.tpl",
            "database/templates/backup/parameters_offensive.tpl",
            "database/templates/backup/parameters_retaliation.tpl",
            "database/templates/backup/parameters_skill.tpl",
            "database/templates/backup/parameters_weaponbonusoffensive.tpl",
            "database/templates/ingameui/hud old.tpl",
            "database/templates/templatebase/copy of characterbio.tpl",
            "database/templates/templatebase/copy of monsterskillmanager.tpl",
        };

        public static bool IsExcluded(Path templateName)
        {
            return s_list.TryGetValue(templateName.ToString(), out var _);
        }
    }
}
