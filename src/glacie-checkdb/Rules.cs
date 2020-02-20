using System;
using System.Collections.Generic;
using System.Text;

namespace Glacie
{
    internal static class Rules
    {
        public static void ProcessDbRecord(DbRecord dbr)
        {
            dbr.CheckResourceReference("itemSkillName");
            dbr.CheckResourceReference("augmentSkillName1");
            dbr.CheckResourceReference("augmentSkillName2");

            CheckSetItem(dbr);
        }

        private static void CheckSetItem(DbRecord dbr)
        {
            if (!dbr.TryGetPropertyValue("templateName", out var templateName)) return;
            if (!StringComparer.OrdinalIgnoreCase.Equals(templateName, @"Database\Templates\ItemSet.tpl")) {
                return;
            }

            if (!dbr.TryGetPropertyValue("setMembers", out var setMembersValue))
            {
                dbr.Context.Report("ER0002:{0}: Item set has no \"setMembers\" property.", dbr.RelativePath);
                return;
            }

            var setMembers = setMembersValue.Split(';');

            foreach (var setMember in setMembers)
            {
                if (dbr.Context.IsResourceExist(setMember))
                {
                    // open resource and check itemSetName
                    var setMemberDbr = dbr.Context.OpenDbRecord(setMember);
                    if (setMemberDbr.TryGetPropertyValue("itemSetName", out var itemSetName))
                    {
                        // Verify back-reference.
                        if (!StringComparer.OrdinalIgnoreCase.Equals(dbr.RelativePath, itemSetName))
                        {
                            dbr.Context.Report("ER0005:{0}: Referenced item \"{1}\" set different `itemSetName`: \"{2}\".", dbr.RelativePath, setMember, itemSetName);
                        }
                    }
                    else
                    {
                        dbr.Context.Report("ER0004:{0}: Referenced item \"{1}\" doesn't set `itemSetName`.", dbr.RelativePath, setMember);
                    }
                }
                else
                {
                    dbr.Context.Report("ER0003:{0}: Item set reference non existent member: \"{1}\".", dbr.RelativePath, setMember);
                }
            }
        }
    }
}
