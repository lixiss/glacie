using System;
using System.Collections.Generic;
using System.Text;

namespace Glacie
{
    internal static class Rules
    {
        public static void ProcessDbRecord(DbRecord dbr)
        {
            dbr.CheckResourceReference("augmentSkillName1");
            dbr.CheckResourceReference("augmentSkillName2");
        }
    }
}
